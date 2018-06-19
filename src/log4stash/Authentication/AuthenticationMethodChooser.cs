using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.Authentication
{
    public class AuthenticationMethodChooser : IAuthenticator
    {
        private IAuthenticator _innerMethod;

        public void AddFilter(IAuthenticator method)
        {
            _innerMethod = method;
        }

        #region Helpers for common authentication methods

        public void AddBasic(BasicAuthenticationMethod method)
        {
            AddFilter(method);
        }

        public void AddAws(AwsAuthenticationMethod method)
        {
            AddFilter(method);
        }

        #endregion

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            if (_innerMethod == null)
            {
                return;
            }
            _innerMethod.Authenticate(client, request);
        }
    }
}
