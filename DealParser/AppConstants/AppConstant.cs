namespace DealParser.AppConstants
{
    public class AppConstant
    {
        public const string APP_SETTINGS_FILENAME = "appsettings.json";
        public const string HOST_GRAPHQL = "https://www.lesegais.ru/open-area/graphql";
        public const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                                         "(KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36";
        
        
        public const int IVALID_SETTINGS_FORMAT    = 1;
        public const int SETTINGS_FILE_NOT_FOUND   = 4;
        public const int UNKNOWN_ERROR             = 5;
    }
}