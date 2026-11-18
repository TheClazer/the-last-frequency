using System;
using System.Collections.Generic;

namespace LF
{
    /// <summary>Minimal QR Code encoder: byte mode, error correction M, versions 1-6 (up to ~106 bytes), mask chosen by the standard penalty rules. Used to show a share URL on the headset.</summary>
    public static class LFQR
    {
        static readonly int[] EccPerBlock = { 10, 16, 26, 18, 24, 16 };   // level M, versions 1..6
        static readonly int[] NumBlocks = { 1, 1, 1, 2, 2, 4 };

        public static bool[,] Encode(string text)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(text); int ver = 0;
            for (int v = 1; v <= 6; v++) { int cap = RawModules(v) / 8 - EccPerBlock[v - 1] * NumBlocks[v - 1]; if (data.Length + 2 <= cap) { ver = v; break; } }
            if (ver == 0) throw new Exception("QR text too long");
            int dataCw = RawModules(ver) / 8 - EccPerBlock[ver - 1] * NumBlocks[ver - 1]; var bits = new List<bool>();
            Append(bits, 4, 4); Append(bits, data.Length, 8); foreach (var b in data) Append(bits, b, 8);
            int capBits = dataCw * 8; Append(bits, 0, Math.Min(4, capBits - bits.Count)); while (bits.Count % 8 != 0) bits.Add(false);
            var cw = new List<byte>(); for (int i = 0; i < bits.Count; i += 8) { int v = 0; for (int k = 0; k < 8; k++) v = (v << 1) | (bits[i + k] ? 1 : 0); cw.Add((byte)v); }
            for (int pad = 0xEC; cw.Count < dataCw; pad ^= 0xEC ^ 0x11) cw.Add((byte)pad);
            var all = Interleave(cw.ToArray(), ver); int size = ver * 4 + 17; var best = default(bool[,]); int bestPen = int.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                var mod = new bool[size, size]; var fn = new bool[size, size]; DrawFunction(mod, fn, ver); DrawCodewords(mod, fn, all, mask); DrawFormat(mod, fn, mask);
                int pen = Penalty(mod); if (pen < bestPen) { bestPen = pen; best = mod; }
            }
            return best;
        }

        static void Append(List<bool> b, int val, int len) { for (int i = len - 1; i >= 0; i--) b.Add(((val >> i) & 1) != 0); }
        static int RawModules(int v)
        {
            int size = v * 4 + 17; int result = size * size - 8 * 8 * 3 - 15 * 2 - 1 - (size - 16) * 2;   // finders+separators, format, dark module, timing
            if (v >= 2) { int n = v / 7 + 2; result -= (n * n - 3) * 25 - (n - 2) * 2 * 20; }
            return result;
        }
        static byte[] Interleave(byte[] data, int ver)
        {
            int nb = NumBlocks[ver - 1], ecc = EccPerBlock[ver - 1]; int raw = RawModules(ver) / 8; int shortBlocks = nb - raw % nb; int shortLen = raw / nb - ecc;
            var blocks = new List<byte[]>(); var eccs = new List<byte[]>(); var rs = RsDivisor(ecc); int k = 0;
            for (int i = 0; i < nb; i++) { int len = shortLen + (i < shortBlocks ? 0 : 1); var d = new byte[len]; Array.Copy(data, k, d, 0, len); k += len; blocks.Add(d); eccs.Add(RsRemainder(d, rs)); }
            var res = new List<byte>(); int maxLen = shortLen + 1;
            for (int i = 0; i < maxLen; i++) for (int j = 0; j < nb; j++) if (i < blocks[j].Length) res.Add(blocks[j][i]);
            for (int i = 0; i < ecc; i++) for (int j = 0; j < nb; j++) res.Add(eccs[j][i]);
            return res.ToArray();
        }
        static byte[] RsDivisor(int degree)
        {
            var r = new byte[degree]; r[degree - 1] = 1; int root = 1;
            for (int i = 0; i < degree; i++) { for (int j = 0; j < r.Length; j++) { r[j] = (byte)Mul(r[j], root); if (j + 1 < r.Length) r[j] ^= r[j + 1]; } root = Mul(root, 2); }
            return r;
        }
        static byte[] RsRemainder(byte[] data, byte[] divisor)
        {
            var r = new byte[divisor.Length];
            foreach (var b in data) { int f = b ^ r[0]; Array.Copy(r, 1, r, 0, r.Length - 1); r[r.Length - 1] = 0; for (int i = 0; i < r.Length; i++) r[i] ^= (byte)Mul(divisor[i], f); }
            return r;
        }
        static int Mul(int x, int y) { int z = 0; for (int i = 7; i >= 0; i--) { z = (z << 1) ^ ((z >> 7) * 0x11D); z ^= ((y >> i) & 1) * x; } return z & 0xFF; }

        static void Set(bool[,] m, bool[,] fn, int x, int y, bool dark) { if (x < 0 || y < 0 || x >= m.GetLength(0) || y >= m.GetLength(0)) return; m[y, x] = dark; fn[y, x] = true; }
        static void DrawFunction(bool[,] m, bool[,] fn, int ver)
        {
            int size = m.GetLength(0);
            for (int i = 0; i < size; i++) { Set(m, fn, 6, i, i % 2 == 0); Set(m, fn, i, 6, i % 2 == 0); }
            Finder(m, fn, 3, 3); Finder(m, fn, size - 4, 3); Finder(m, fn, 3, size - 4);
            if (ver >= 2)
            {
                int n = ver / 7 + 2; int step = ver == 32 ? 26 : (ver * 4 + n * 2 + 1) / (n * 2 - 2) * 2; var pos = new List<int> { 6 }; for (int p = size - 7; pos.Count < n; p -= step) pos.Insert(1, p);
                for (int a = 0; a < n; a++) for (int b = 0; b < n; b++) { if ((a == 0 && b == 0) || (a == 0 && b == n - 1) || (a == n - 1 && b == 0)) continue; Align(m, fn, pos[a], pos[b]); }
            }
            DrawFormat(m, fn, 0);
        }
        static void Finder(bool[,] m, bool[,] fn, int cx, int cy) { for (int dy = -4; dy <= 4; dy++) for (int dx = -4; dx <= 4; dx++) { int d = Math.Max(Math.Abs(dx), Math.Abs(dy)); Set(m, fn, cx + dx, cy + dy, d != 2 && d != 4); } }
        static void Align(bool[,] m, bool[,] fn, int cx, int cy) { for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) Set(m, fn, cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1); }
        static void DrawFormat(bool[,] m, bool[,] fn, int mask)
        {
            int size = m.GetLength(0); int data = (0 << 3) | mask; int rem = data; for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537); int bits = ((data << 10) | rem) ^ 0x5412;
            for (int i = 0; i <= 5; i++) Set(m, fn, 8, i, ((bits >> i) & 1) != 0); Set(m, fn, 8, 7, ((bits >> 6) & 1) != 0); Set(m, fn, 8, 8, ((bits >> 7) & 1) != 0); Set(m, fn, 7, 8, ((bits >> 8) & 1) != 0); for (int i = 9; i < 15; i++) Set(m, fn, 14 - i, 8, ((bits >> i) & 1) != 0);
            for (int i = 0; i < 8; i++) Set(m, fn, size - 1 - i, 8, ((bits >> i) & 1) != 0); for (int i = 8; i < 15; i++) Set(m, fn, 8, size - 15 + i, ((bits >> i) & 1) != 0); Set(m, fn, 8, size - 8, true);
        }
        static void DrawCodewords(bool[,] m, bool[,] fn, byte[] data, int mask)
        {
            int size = m.GetLength(0); int i = 0;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < size; vert++) for (int j = 0; j < 2; j++)
                {
                    int x = right - j; bool up = ((right + 1) & 2) == 0; int y = up ? size - 1 - vert : vert;
                    if (!fn[y, x] && i < data.Length * 8) { m[y, x] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0; i++; }
                    if (!fn[y, x]) { bool inv; switch (mask) { case 0: inv = (x + y) % 2 == 0; break; case 1: inv = y % 2 == 0; break; case 2: inv = x % 3 == 0; break; case 3: inv = (x + y) % 3 == 0; break; case 4: inv = (x / 3 + y / 2) % 2 == 0; break; case 5: inv = x * y % 2 + x * y % 3 == 0; break; case 6: inv = (x * y % 2 + x * y % 3) % 2 == 0; break; default: inv = ((x + y) % 2 + x * y % 3) % 2 == 0; break; } if (inv) m[y, x] = !m[y, x]; }
                }
            }
        }
        static int Penalty(bool[,] m)
        {
            int n = m.GetLength(0), res = 0;
            for (int y = 0; y < n; y++) { int run = 1; for (int x = 1; x < n; x++) { if (m[y, x] == m[y, x - 1]) { run++; if (run == 5) res += 3; else if (run > 5) res++; } else run = 1; } }
            for (int x = 0; x < n; x++) { int run = 1; for (int y = 1; y < n; y++) { if (m[y, x] == m[y - 1, x]) { run++; if (run == 5) res += 3; else if (run > 5) res++; } else run = 1; } }
            for (int y = 0; y < n - 1; y++) for (int x = 0; x < n - 1; x++) if (m[y, x] == m[y, x + 1] && m[y, x] == m[y + 1, x] && m[y, x] == m[y + 1, x + 1]) res += 3;
            int dark = 0; foreach (var b in m) if (b) dark++; int total = n * n; int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1; res += k * 10; return res;
        }
    }
}
