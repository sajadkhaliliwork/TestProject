using HtmlAgilityPack;
using HtmlConvert.Contracts;
using NReco.Text;

namespace HtmlConvert.Implementing;


public class AhoCorasick : IConvertorHtml
{
    private readonly ILinkProvider _repository;

    public AhoCorasick(ILinkProvider repository)
    {
        this._repository = repository;

    }

    public string Convert(string html)
    {
        var links = _repository.GetLinks().OrderByDescending(x => x.Key).ToDictionary(k => k.Key.Trim(), v => v.Value.Trim());

        var trie = new AhoCorasickDoubleArrayTrie<string>();
        trie.Build(links.OrderByDescending(x => x.Key).ToDictionary(k => k.Key, v => $" {v.Key} "));

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        ReplaceTextWithLinks(doc.DocumentNode, links, trie);
        return doc.DocumentNode.OuterHtml; // HtmlEntity.DeEntitize(doc.DocumentNode.OuterHtml)

    }

    void ReplaceTextWithLinks(HtmlNode node, Dictionary<string, string> linkTable, AhoCorasickDoubleArrayTrie<string> trie)
    {
        if (IsLinkElement(node)) return;

        if (node.NodeType == HtmlNodeType.Text)
        {

            if (node.ParentNode != null && IsLinkElement(node.ParentNode)) return;

            // string originalText = node.InnerText;
            string originalText = HtmlEntity.DeEntitize(node.InnerText);

            // trie
            var matches = trie.ParseText(originalText);
            if (!matches.Any()) return;

            HashSet<string> usedKeys = new();

            var replacements = matches
                .OrderBy(m => m.Begin).ThenByDescending(m => m.End)
                .Select(m => (Start: m.Begin, End: m.End, Key: m.Value))
                .Where(match => !usedKeys.Contains(match.Key)).ToList();

            foreach (var m in replacements)
                usedKeys.Add(m.Key);

            HtmlNode currentNode = node;
            int lastPos = 0;

            foreach (var (start, end, key) in replacements)
            {
                if (start < lastPos)
                    continue;


                string beforeText = originalText.Substring(lastPos, start - lastPos);
                string linkText = string.Empty;

                if (start < 0 || start >= originalText.Length)
                {
                    Console.WriteLine($"اندیس نامعتبر: start={start}, end={end}, طول رشته={originalText.Length}");
                    continue;
                }

                if (end >= originalText.Length)
                {
                    linkText = originalText.Substring(start);
                    lastPos = originalText.Length;
                }
                else
                {
                    linkText = originalText.Substring(start, end - start);
                    lastPos = end + 1;
                }

                if (!string.IsNullOrEmpty(beforeText))
                {
                    HtmlNode beforeNode = HtmlNode.CreateNode(beforeText);
                    currentNode.ParentNode.InsertBefore(beforeNode, currentNode);
                }

                if (!linkTable.TryGetValue(key.Trim(), out string url))
                {
                    Console.WriteLine($"کلید '{key}' در linkTable یافت نشد.");
                    continue;
                }

                HtmlNode linkNode = HtmlNode.CreateNode($"<a href='{url}'> {linkText} </a>");
                currentNode.ParentNode.InsertBefore(linkNode, currentNode);
            }

            if (lastPos < originalText.Length)
            {
                HtmlNode afterNode = HtmlNode.CreateNode(originalText.Substring(lastPos));
                currentNode.ParentNode.InsertBefore(afterNode, currentNode);
            }
            currentNode.ParentNode.RemoveChild(currentNode);
        }
        else
        {

            foreach (var child in node.ChildNodes.ToList())
                ReplaceTextWithLinks(child, linkTable, trie);
        }
    }


    private bool IsLinkElement(HtmlNode node)
    {
        return node.NodeType == HtmlNodeType.Element &&
               string.Equals(node.Name, "a", StringComparison.OrdinalIgnoreCase);
    }

}
