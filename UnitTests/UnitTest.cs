using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApiEncode;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {

        private class Code_WebAdapter
        {
            public string Encrypt(string text, string key)
            {
                try
                {

                    foreach (char c in key)
                    {
                        if (!char.IsLetter(c))
                            return "";
                    }
  
                    foreach (char c in text)
                    {
                        if (!char.IsLetter(c))
                            return "";
                    }

                    return EncryptionService.Encrypt(text.ToLower(), key.ToLower());
                }
                catch
                {
                    return "";
                }
            }

            public string Decrypt(string text, string key)
            {
                try
                {

                    foreach (char c in key)
                    {
                        if (!char.IsLetter(c))
                            return "";
                    }

                    foreach (char c in text)
                    {
                        if (!char.IsLetter(c))
                            return "";
                    }

                    return EncryptionService.Decrypt(text.ToLower(), key.ToLower());
                }
                catch
                {
                    return "";
                }
            }

            public IResult Login(string login, string password)
            {
                if (string.IsNullOrEmpty(login))
                {
                    return Results.BadRequest(new { Error = "Invalid login" });
                }
                
                var successResponse = new
                {
                    Token = "fake-jwt-token-12345",
                    UserId = 1,
                    Email = login
                };
                
                return Results.Ok(successResponse);
            }
        }


        [TestMethod]
        public void TestEncryptTrue()
        {
            Code_WebAdapter code = new Code_WebAdapter();

            const string text = "qwerty";
            const string key = "abc";

            string result = code.Encrypt(text, key);

            Assert.IsTrue(result == "qxgrua");
        }

        [TestMethod]
        public void TestEncryptFalse()
        {
            Code_WebAdapter code = new Code_WebAdapter();

            const string text = "qwerty";
            const string key = "abc123"; 

            string result = code.Encrypt(text, key);

            Assert.IsTrue(result == "");
        }

        [TestMethod]
        public void TestDecryptTrue()
        {

            Code_WebAdapter code = new Code_WebAdapter();

            const string text = "qxgrua";
            const string key = "abc";

            string result = code.Decrypt(text, key);

            Assert.IsTrue(result == "qwerty");
        }

        [TestMethod]
        public void TestDecryptFalse()
        {
            Code_WebAdapter code = new Code_WebAdapter();

            const string text = "qxgrua123";
            const string key = "abc";

            // Act
            string result = code.Decrypt(text, key);

            // Assert
            Assert.IsTrue(result == "");
        }


        [TestMethod]
        public void TestTokenTrue()
        {
            Code_WebAdapter code = new Code_WebAdapter();

            const string login = "user";
            const string password = "password";

            var result = code.Login(login, password);
            
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<IResult>(result);

            var value = result.GetType().GetProperty("Value");
            Assert.IsNotNull(value);
            
            var response = value.GetValue(result);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void TestTokenFalse()
        {
            Code_WebAdapter code = new Code_WebAdapter();

            const string login = ""; 
            const string password = "password";

            var result = code.Login(login, password);
            
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<IResult>(result);
            
            var statusCodeProp = result.GetType().GetProperty("StatusCode");
            Assert.IsNotNull(statusCodeProp);
            
            var statusCode = (int)statusCodeProp.GetValue(result);
            Assert.AreEqual(400, statusCode);
        }
    }
}