// Services/HttpService.cs
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ModPC_Gui.Models;

namespace ModPC_Gui.Services
{
    public class HttpService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<(bool success, string error, LoginResult result)> PELoginAsync(string content)
        {
            try
            {
                // 构造PE认证请求
                var request = new PEAURequest
                {
                    sa_data = App.ReadJson("sa_data_pe.json"),
                    engine_version = App.Temp.engineVersion,
                    patch_version = App.Temp.patchVersion,
                    message = GenerateMessage(out var seed),
                    sauth_json = content,
                    seed = seed,
                    sign = GenerateSign(seed)
                };

                // 加密请求
                var encryptedContent = x19Crypt.HttpEncrypt_g79v12(
                    JsonSerializer.SerializeToUtf8Bytes(request));

                // 发送请求
                var response = await _httpClient.PostAsync(
                    $"{App.Temp.PE_url}/pe-authentication",
                    new StringContent(encryptedContent.ToHex(), Encoding.UTF8, "application/json"));

                // 处理响应
                var responseString = await response.Content.ReadAsStringAsync();
                var responseBytes = responseString.HexToBytes();
                var decryptedResponse = x19Crypt.HttpDecrypt_g79v12_(responseBytes);

                var result = JsonSerializer.Deserialize<LoginResult>(decryptedResponse);

                if (result?.code == 0)
                {
                    return (true, null, result);
                }

                return (false, result?.message ?? "登录失败", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        private string GenerateMessage(out string seed)
        {
            seed = Guid.NewGuid().ToString();
            return $"{App.Temp.engineVersion}{App.Temp.libminecraftpe}" +
                   $"{App.Temp.patchVersion}{App.Temp.patch}{seed}";
        }

        private string GenerateSign(string seed)
        {
            var signBytes = CoreSign.PESignCount(
                $"{App.Temp.engineVersion}{App.Temp.libminecraftpe}" +
                $"{App.Temp.patchVersion}{App.Temp.patch}{seed}");

            return Convert.ToBase64String(signBytes);
        }
    }

    public static class CoreSign
    {
        [DllImport("Auth.Sign.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CountSign(byte[] message, int size, int offset, int rounds);

        [DllImport("Auth.Sign.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeMemory(IntPtr ptr);

        public static byte[] PESignCount(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            var resultPtr = CountSign(bytes, bytes.Length, App.Temp.offset, App.Temp.rounds);

            if (resultPtr != IntPtr.Zero)
            {
                var result = new byte[16];
                Marshal.Copy(resultPtr, result, 0, 16);
                FreeMemory(resultPtr);
                return result;
            }

            return null;
        }
    }

    public static class x19Crypt
    {
        public static byte[] HttpEncrypt_g79v12(byte[] bodyIn, int lengthFill = 16)
        {
            try
            {
                // 填充数据
                int paddedLength = (int)Math.Ceiling((bodyIn.Length + lengthFill) / 16.0) * 16;
                var paddedBody = new byte[paddedLength];
                Array.Copy(bodyIn, paddedBody, bodyIn.Length);

                var randomBytes = RandomBytes(lengthFill);
                for (int i = 0; i < randomBytes.Length; i++)
                {
                    paddedBody[bodyIn.Length + i] = randomBytes[i];
                }

                // 生成密钥和IV
                var random = new Random();
                var query = (byte)((random.Next(0, 15) << 4) | 0xC);
                var iv = RandomBytes(16);
                var key = PickKey_g79v12(query);

                // AES加密
                var encrypted = AESHelper.AES_CBC_Encrypt(key, paddedBody, iv);

                // 组合结果
                var result = new byte[iv.Length + encrypted.Length + 1];
                Array.Copy(iv, 0, result, 0, iv.Length);
                Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);
                result[result.Length - 1] = query;

                return result;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        public static string HttpDecrypt_g79v12_(byte[] body)
        {
            if (body.Length < 18) return null;

            try
            {
                // 提取IV和加密数据
                var iv = new byte[16];
                Array.Copy(body, 0, iv, 0, 16);

                var encrypted = new byte[body.Length - 17];
                Array.Copy(body, 16, encrypted, 0, encrypted.Length);

                var query = body[body.Length - 1];

                // 解密
                var decrypted = AESHelper.AES_CBC_Decrypt(
                    PickKey_g79v12(query), encrypted, iv);

                // 移除填充
                int end = decrypted.Length - 1;
                while (end >= 0 && decrypted[end] == 0)
                {
                    end--;
                }

                return Encoding.UTF8.GetString(decrypted, 0, end + 1);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] PickKey_g79v12(byte query)
        {
            var keys = new[]
            {
                "60F1E0D1FD635362430747215CF1C2FF",
                "EA5B62D27D0338374852C4B9469D7AC6",
                "17238D55501C5F020B155FB3303591E6",
                "8C5CEAE0F443E006A050266F73ADD5B0",
                "1C02CE22FB22F0E72060217418F351F3",
                "9A01773FEBB0CFE0EBDBF37F4D23C27F",
                "43F32300BF252CC320E2572ACE766367",
                "07F161011B3101F1ED0301735631E734",
                "0454E7707A5F37565601E100406060AF",
                "647554BAD3100C43C16660F002CC10F3",
                "E157213170F842382032564265B0B043",
                "914FC59311B04151393EF6896A847636",
                "0710C0205D224237025323265C145FA1",
                "054E6F01165267025C3111F562A921E9",
                "722D1789E792E2CA0D5322211FD0F5AE",
                "91F7C751FCF671F34943430772341799"
            };

            return HexStringToByteArray(keys[(query >> 4) & 0xF]);
        }

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            new Random().NextBytes(bytes);
            return bytes;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }

    public static class AESHelper
    {
        public static byte[] AES_CBC_Encrypt(byte[] key, byte[] data, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] AES_CBC_Decrypt(byte[] key, byte[] data, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }

    public static class Extensions
    {
        public static string ToHex(this byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        public static byte[] HexToBytes(this string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }

    public class PEAURequest
    {
        [JsonPropertyName("sa_data")]
        public string sa_data { get; set; }

        [JsonPropertyName("engine_version")]
        public string engine_version { get; set; }

        [JsonPropertyName("patch_version")]
        public string patch_version { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("sauth_json")]
        public string sauth_json { get; set; }

        [JsonPropertyName("seed")]
        public string seed { get; set; }

        [JsonPropertyName("sign")]
        public string sign { get; set; }
    }
}