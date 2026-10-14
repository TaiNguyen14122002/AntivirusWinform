// Chỉ dùng bởi runner Linux, không biên dịch vào ứng dụng Windows.
// Mono 6.8 có lỗi dispose buffer chưa tạo drawable. Thêm kiểm tra null
// vào bản framework riêng trong thư mục test; không sửa framework hệ thống.
using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class MonoRendererCompat
{
    static int Main(string[] args)
    {
        using (var assembly = AssemblyDefinition.ReadAssembly(args[0]))
        {
            var type = assembly.MainModule.Types.Single(t => t.Name == "XplatUIX11");
            var method = type.Methods.Single(m => m.Name == "DestroyOffscreenDrawable" && m.Parameters.Count == 1);
            var first = method.Body.Instructions[0];
            var il = method.Body.GetILProcessor();
            il.InsertBefore(first, Instruction.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
            il.InsertBefore(first, Instruction.Create(OpCodes.Brtrue, first));
            il.InsertBefore(first, Instruction.Create(OpCodes.Ret));
            assembly.Write(args[1]);
        }
        Console.WriteLine("Mono test renderer: guarded cleanup of empty drawable.");
        return 0;
    }
}
