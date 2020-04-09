using RestSharp.Authenticators;

namespace log4stash.Authentication
{
    public interface IAuthenticationMethodChooser : IAuthenticator
    {
        void AddFilter(IAuthenticator method);
        void AddBasic(BasicAuthenticationMethod method);
        void AddAws(AwsAuthenticationMethod method);
    }
}