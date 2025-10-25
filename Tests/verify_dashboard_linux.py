#!/usr/bin/env python3
"""Build nguồn .NET Framework bằng Roslyn và chạy WinForms thật bằng Mono/Xvfb.

Yêu cầu: python3, dotnet SDK, mono-devel, libgdiplus, Xvfb.
SQL chỉ bị vô hiệu trong DashboardSmoke; mã ứng dụng giữ nguyên kết nối SQL.
"""
from pathlib import Path
import os
import re
import socket
import subprocess
import sys
import time
import xml.etree.ElementTree as ET


def main():
    root = Path(__file__).resolve().parent.parent
    output = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root / "DashboardVerification"
    output.mkdir(parents=True, exist_ok=True)
    reference_root = Path("/usr/lib/mono/4.7.2-api")
    if not reference_root.is_dir():
        raise RuntimeError("Cần reference assemblies .NET Framework 4.7.2 của mono-devel.")
    sdk_lines = subprocess.check_output(["dotnet", "--list-sdks"], text=True).strip().splitlines()
    version, sdk_root = re.match(r"(\S+) \[(.+)\]", sdk_lines[-1]).groups()
    compiler = Path(sdk_root) / version / "Roslyn/bincore/csc.dll"
    references = ["mscorlib", "System", "System.Core", "System.Drawing", "System.Windows.Forms",
                  "System.Data", "System.Xml", "System.Xml.Linq", "System.Net.Http",
                  "System.Management", "System.Configuration", "Microsoft.CSharp",
                  "Microsoft.VisualBasic", "System.Data.DataSetExtensions", "System.Deployment"]
    common = ["/nologo", "/langversion:7.3", "/nostdlib+"]
    common += ["/reference:" + str(reference_root / (name + ".dll")) for name in references]
    ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
    project = ET.parse(root / "ScanAndRemoveVirus.csproj")
    executable = output / "ScanAndRemoveVirus.exe"
    app_args = common + ["/target:winexe", "/main:ScanAndRemoveVirus.Program", "/out:" + str(executable)]
    for entry in project.findall(".//m:Compile", ns):
        app_args.append(str(root / entry.attrib["Include"].replace("\\", "/")))
    for entry in project.findall(".//m:EmbeddedResource", ns):
        source = root / entry.attrib["Include"].replace("\\", "/")
        name = "ScanAndRemoveVirus." + str(source.relative_to(root)).replace("/", ".")[:-5] + ".resources"
        resource = output / name
        subprocess.run(["resgen", str(source), str(resource)], check=True, capture_output=True, text=True)
        app_args.append("/resource:" + str(resource) + "," + name)
    app_response = output / "app.rsp"
    app_response.write_text("\n".join('"' + arg + '"' for arg in app_args), encoding="utf-8")
    built = subprocess.run(["dotnet", str(compiler), "@" + str(app_response)], capture_output=True, text=True)
    (output / "build.log").write_text(built.stdout + built.stderr, encoding="utf-8")
    if built.returncode:
        print(built.stdout + built.stderr)
        return built.returncode
    smoke = output / "dashboard-test.exe"
    smoke_args = common + ["/target:exe", "/out:" + str(smoke), "/reference:" + str(executable),
                           str(root / "Tests/DashboardSmoke.cs"), str(root / "Tests/DashboardSmoke.Sketch.cs")]
    compiled = subprocess.run(["dotnet", str(compiler)] + smoke_args, capture_output=True, text=True)
    (output / "test-build.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    if compiled.returncode:
        print(compiled.stdout + compiled.stderr)
        return compiled.returncode

    sketch_smoke = output / "sketch20-test.exe"
    sketch_args = common + ["/target:exe", "/out:" + str(sketch_smoke), "/reference:" + str(executable), str(root / "Tests/Sketch20Smoke.cs"), str(root / "Tests/Sketch20BridgeSmoke.cs")]
    sketch_built = subprocess.run(["dotnet", str(compiler)] + sketch_args, capture_output=True, text=True)
    (output / "sketch20-build.log").write_text(sketch_built.stdout + sketch_built.stderr, encoding="utf-8")
    if sketch_built.returncode:
        print(sketch_built.stdout + sketch_built.stderr)
        return sketch_built.returncode

    typography = output / "typography-test.exe"
    typography_args = common + ["/target:exe", "/out:" + str(typography), "/reference:" + str(executable), str(root / "Tests/Sketch20TypographySmoke.cs")]
    typography_built = subprocess.run(["dotnet", str(compiler)] + typography_args, capture_output=True, text=True)
    (output / "typography-build.log").write_text(typography_built.stdout + typography_built.stderr, encoding="utf-8")
    if typography_built.returncode:
        print(typography_built.stdout + typography_built.stderr)
        return typography_built.returncode

    # Mono 6.8: dùng một framework copy có null-guard cho buffer chưa tạo drawable.
    # Đây là điều chỉnh renderer test, không đi vào project hay bản chạy Windows.
    cecil = sorted(Path("/usr/lib/mono/gac/Mono.Cecil").glob("0.11*/Mono.Cecil.dll"))
    if cecil and Path("/usr/lib/mono/4.5/System.Windows.Forms.dll").is_file():
        compat = output / "mono-renderer-compat.exe"
        subprocess.run(["mcs", "-r:" + str(cecil[0]), "-out:" + str(compat),
                        str(root / "Tests/MonoRendererCompat.cs")], check=True, capture_output=True, text=True)
        subprocess.run(["mono", str(compat), "/usr/lib/mono/4.5/System.Windows.Forms.dll",
                        str(output / "System.Windows.Forms.dll")], check=True)

    # TCP localhost dùng được trong môi trường không hỗ trợ Unix socket.
    display = None
    for candidate in range(87, 97):
        with socket.socket() as probe:
            try:
                probe.bind(("127.0.0.1", 6000 + candidate))
                display = candidate
                break
            except OSError:
                continue
    if display is None:
        raise RuntimeError("Không có display trống để chạy Xvfb.")
    with (output / "xvfb.log").open("w") as server_log:
        server = subprocess.Popen(["Xvfb", ":" + str(display), "-screen", "0", "1920x1080x24", "-ac",
                                   "-listen", "tcp", "-nolisten", "unix", "-nolisten", "local"],
                                  stdout=server_log, stderr=server_log)
        try:
            ready = False
            for attempt in range(30):
                if server.poll() is not None:
                    break
                try:
                    with socket.create_connection(("127.0.0.1", 6000 + display), timeout=0.2):
                        ready = True
                        break
                except OSError:
                    time.sleep(0.1)
            if not ready:
                raise RuntimeError("Xvfb chưa sẵn sàng; xem xvfb.log.")
            env = os.environ.copy()
            env["DISPLAY"] = "127.0.0.1:" + str(display)
            env["XVIRUS_LEGACY_UI_TEST"] = "1"
            with (output / "dashboard-test.log").open("w") as result_log:
                result = subprocess.run(["mono", str(smoke), str(output / "screenshots")],
                                        env=env, stdout=result_log, stderr=result_log, timeout=120)
            with (output / "sketch20-test.log").open("w") as sketch_log:
                sketch_result = subprocess.run(["mono", str(sketch_smoke), str(output / "20-screens")], env=env, stdout=sketch_log, stderr=sketch_log, timeout=120)
            env["XVIRUS_LEGACY_UI_TEST"] = "0"
            with (output / "typography-test.log").open("w") as typography_log:
                typography_result = subprocess.run(["mono", str(typography), str(output / "typography")], env=env, stdout=typography_log, stderr=typography_log, timeout=120)

        finally:
            server.terminate()
            server.wait(timeout=5)
    print((output / "dashboard-test.log").read_text(encoding="utf-8"))
    print((output / "sketch20-test.log").read_text(encoding="utf-8"))
    print((output / "typography-test.log").read_text(encoding="utf-8"))
    return result.returncode or sketch_result.returncode or typography_result.returncode


if __name__ == "__main__":
    sys.exit(main())
