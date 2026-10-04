using OmniFile.Core;

namespace OmniFile.Sample
{
    public class DomainEntityOneStorable : IStorable
    {
        private int id;

        public DomainEntityOneStorable(int entityId)
        {
            id = entityId;
        }

        public static string StorageCategory => "DomainEntity1";

        public string Category => StorageCategory;

        public string Key => id.ToString("D9") + ".txt";

        public string ContentType => "text/plain";

    }
}
