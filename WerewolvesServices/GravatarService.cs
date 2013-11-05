using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class GravatarService
{
	private static readonly string _GRAVATAR = "http://gravatar.com/avatar/";
	/// Hashes an email with MD5.  Suitable for use with Gravatar profile
	/// image urls
	public static string Get(string email, int size)
	{
		// Create a new instance of the MD5CryptoServiceProvider object.  
		MD5 md5Hasher = MD5.Create();

		// Convert the input string to a byte array and compute the hash.  
		byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email));

		// Create a new Stringbuilder to collect the bytes  
		// and create a string.  
		StringBuilder sBuilder = new StringBuilder();

		// Loop through each byte of the hashed data  
		// and format each one as a hexadecimal string.  
		for (int i = 0; i < data.Length; i++)
		{
			sBuilder.Append(data[i].ToString("x2"));
		}

		return _GRAVATAR + sBuilder.ToString() + "?s=20";  // Return the hexadecimal string. 
	}
	public async Task<string> EncodeGravatar(string url)
	{
			string result = "";
			using (var client = new HttpClient())
			using (var s = await client.GetStreamAsync(url))
			using (var reader = new StreamReader(s))
			{
				result = await reader.ReadToEndAsync();
			}

			return "data:image/png;base64," + Convert.ToBase64String(Encoding.Default.GetBytes(result));
	}
}