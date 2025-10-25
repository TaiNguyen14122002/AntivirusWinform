"""Generate hash and rolling fingerprint for a *verified* chunk, using only Python stdlib.

Usage: python chunk_signature_fingerprint.py path/to/file --offset 256 --size 64
Reads bytes only; never executes the target file or writes database records.
"""
import argparse
import hashlib
from pathlib import Path

BASE = 257
MASK64 = (1 << 64) - 1


def fingerprint(data):
    value = 0
    for byte in data:
        value = (value * BASE + byte + 1) & MASK64
    return value


def main():
    parser = argparse.ArgumentParser(description="Create chunk SHA-256 + FastHash from verified raw bytes")
    parser.add_argument("file", type=Path)
    parser.add_argument("--offset", type=int, required=True)
    parser.add_argument("--size", type=int, required=True)
    args = parser.parse_args()
    if args.offset < 0 or not (8 <= args.size <= 65536):
        parser.error("offset must be non-negative and size must be from 8 to 65536 bytes")
    with args.file.open("rb") as source:
        source.seek(args.offset)
        data = source.read(args.size)
    if len(data) != args.size:
        parser.error("file too short for requested offset and chunk size")
    print("ChunkSize:", args.size)
    print("OriginalOffset:", args.offset)
    print("ChunkHash:", hashlib.sha256(data).hexdigest())
    print("FastHash:", "0x%016X" % fingerprint(data))
    print("Store the SHA-256 and fingerprint from the SAME verified chunk bytes.")


if __name__ == "__main__":
    main()
