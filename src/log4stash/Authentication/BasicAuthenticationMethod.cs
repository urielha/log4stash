using System;
using System.Text;

namespace log4stash.Authentication
{
    public class BasicAuthenticationMethod : IAuthenticationMethod
    {
        public string Password { get; set; }

        public string Username { get; set; }

        public string CreateAuthenticationHeader(RequestData requestData)
        {
            var authInfo = string.Format("{0}:{1}", Username, Password);
            var encodedAuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
            var authorizationHeaderValue = string.Format("{0} {1}", "Basic", encodedAuthInfo);
            return authorizationHeaderValue;
        }
    }
}