
namespace ZBand_EZTV
{
    public class FirstRef
    {
        public string type { get; set; }
        public string @ref { get; set; }
    }

    public class LastRef
    {
        public string type { get; set; }
        public string @ref { get; set; }
    }

    public class Link
    {
        public string type { get; set; }
        public string @ref { get; set; }
    }

    public class NextRef
    {
        public string type { get; set; }
        public string @ref { get; set; }
    }

    public class PrevRef
    {
        public string type { get; set; }
        public object @ref { get; set; }
    }
}
