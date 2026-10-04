using OmniFile.Core;

namespace OmniFile.Sample
{
    public class DomainEntityTwoStorable : IStorable
    {
        private int id;

        public DomainEntityTwoStorable(int entityId)
        {
            id = entityId;
        }

        public static string StorageCategory => "DomainEntity2";

        public string Category => StorageCategory;

        public string Key => id.ToString("D9") + ".txt";

        public string ContentType => "text/plain";
    }
}
