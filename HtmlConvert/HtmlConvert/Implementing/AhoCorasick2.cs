using HtmlAgilityPack;
using HtmlConvert.Contracts;
using NReco.Text;
using System.Collections;
using System.Collections.Generic;

namespace HtmlConvert.Implementing;

public class AhoCorasick2 : IConvertorHtml
{
    Dictionary<string, string> links;
    AhoCorasickDoubleArrayTrie<string> trie;
    HashSet<string> usedKeys;
    public AhoCorasick2(ILinkProvider provider)
    {
 
        links = provider.GetLinks().OrderByDescending(x => x.Key).ToDictionary(k => $"{k.Key.Trim()}", v => v.Value.Trim());
           trie = new AhoCorasickDoubleArrayTrie<string>();
        trie.Build(links.OrderByDescending(x => x.Key).ToDictionary(k=> $"{k.Key}", v=>$"{v.Value}")   );
            
           // .ToDictionary(k => k.Key , v => $"{k.Key}"));

    }

    public string Convert(string html)
    {
        usedKeys = new();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        ReplaceTextWithLinks(doc.DocumentNode);
        return HtmlEntity.DeEntitize(doc.DocumentNode.OuterHtml);

    }

    void ReplaceTextWithLinks(HtmlNode node)
    {
        if (IsLinkElement(node)) return;

        if (node.NodeType == HtmlNodeType.Text)
        {
            if (node.ParentNode != null && IsLinkElement(node.ParentNode)) return;

            ProcessNode(node);
        }
        else
        {

            foreach (var child in node.ChildNodes.ToList())
                ReplaceTextWithLinks(child);
        }
    }

    private void ProcessNode2(HtmlNode node)
    {
         //string originalText = node.InnerText;
        string originalText = $" { HtmlEntity.DeEntitize(node.InnerText)} ";

        #region  trie
        var matches = trie.ParseText (originalText);
        if (!matches.Any()) return;

        Console.WriteLine($"originalText={originalText} originalText.Length={originalText.Length}");

        var replacements = matches
            //.OrderBy(m => m.Begin).ThenByDescending(m => m.End)
            .OrderByDescending(x=>x.Value)
            .Select(m => (Start: m.Begin, End: m.End, Key: m.Value))
            .Where(match => !usedKeys.Contains(match.Key)).ToList();



        #endregion  trie

        HtmlNode currentNode = node;
        int lastPos = 0;

        foreach (var (start, end, key) in replacements)
        {
            if (usedKeys.Contains(key))
                continue;
  

            IEnumerable<string> collection = links.Where(x=>key.Contains( x.Key)).Select(x=>x.Key);
            foreach (var item in collection)
            {
                usedKeys.Add(item);
            }
           // usedKeys.Add(key);
            if (start < lastPos)
                continue;

            #region  calculatePosition
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
                lastPos = end ;
            }
            #endregion  calculatePosition

            #region beforelinkNode

            if (!string.IsNullOrEmpty(beforeText))
            {
                AddNode(currentNode, beforeText);
            }
            #endregion   beforelinkNode

            #region linkNode
            if (!links.TryGetValue(key.Trim(), out string url))
            {
                Console.WriteLine($"کلید '{key}' در linkTable یافت نشد.");
                continue;
            }
         
            var linkNodeText = $"<a href='{url}'> {linkText} </a>";
             AddNode(currentNode, linkNodeText);
            #endregion   linkNode
        }
        #region afterlinkNode
        if (lastPos < originalText.Length)
        {

            var afterNodeText = originalText.Substring(lastPos);
            AddNode(currentNode, afterNodeText);
        }
        #endregion   afterlinkNode

        currentNode.ParentNode.RemoveChild(currentNode);

    
    }
     void AddNode(HtmlNode refChild, string nodeText)
    {
        HtmlNode beforeNode = HtmlNode.CreateNode(nodeText);
        refChild.ParentNode.InsertBefore(beforeNode, refChild);
    }
     bool IsLinkElement(HtmlNode node)
    {
        return node.NodeType == HtmlNodeType.Element &&
               string.Equals(node.Name, "a", StringComparison.OrdinalIgnoreCase);
    }

    private void ProcessNode(HtmlNode node)
    {
        string originalText = $"{HtmlEntity.DeEntitize(node.InnerText)}";
        Console.WriteLine($"Processing Node: {originalText}");

        var matches = trie.ParseText(originalText);
        if (!matches.Any()) return;

        List<(int Start, int End, string Key)> replacements = matches


            .Where(m => !matches
     .Any(othermatches => m.Value != othermatches.Value && othermatches.Value.Contains(m.Value)))
            .Where(match => !usedKeys.Contains(match.Value))

                        .OrderBy(x => x.Begin) // Ensure replacements are applied in the correct order
                .Select(m => (Start: m.Begin, End: m.End, Key:  m.Value   ))
            .ToList();


     // var ilteredArticles = matches.Where(m => !matches
     //.Any(othermatches => m.Value != othermatches.Value && othermatches.Value.Contains(m.Value)))
     //.ToList();

     



        
        HtmlNode currentNode = node;
        int lastPos = 0;

        foreach (var (start, end, key) in replacements)
        {
            if (usedKeys.Contains(key))
                continue;

            //IEnumerable<string> collection = links.Where(x => key.Contains(x.Key)).Select(x => x.Key);
            //foreach (var item in collection)
            //{
            //    usedKeys.Add(item);
            //}

            if (start < lastPos)
                continue;

            string beforeText = originalText.Substring(lastPos, start - lastPos);
            string linkText = string.Empty;

            if (start < 0 || start >= originalText.Length)
            {
                Console.WriteLine($"Invalid index: start={start}, end={end}, length={originalText.Length}");
                continue;
            }

            if (end >= originalText.Length)
            {
                if (start != 0)
                {
                    var d = originalText.Substring( start-1, 1);
                    if (string.IsNullOrEmpty(d) || d == "" || d == " ")
                    { }
                    else
                        continue;
                    
                   // if (d != "" || d != " ") continue;
                }

                linkText = originalText.Substring(start);
              
                lastPos = originalText.Length;
            }
            else
            {
          
                if(start==0)
                {
                  

                    var dq = originalText.Substring(end , 1);
                    if (string.IsNullOrEmpty(dq) || dq == "" || dq == " ")
                    { }
                    else
                        continue;
                 
                    //if (d != "" || d != " " || string.IsNullOrEmpty(d)) continue;
                }

                else
                {
                    var d = originalText.Substring(start-1, 1);
                    if (string.IsNullOrEmpty(d) || d == "" || d == " ")
                    { }
                    else
                        continue;

                    var dq = originalText.Substring(end, 1);
                    if (string.IsNullOrEmpty(dq) || dq == "" || dq == " ")
                    { }
                    else
                        continue;
                }


                    linkText = originalText.Substring(start, end - start);
                lastPos = end;
            }

            if (!string.IsNullOrEmpty(beforeText))
            {
                AddNode(currentNode, beforeText);
            }

            if (!links.TryGetValue(key.Trim(), out string url))
            {
                Console.WriteLine($"Key '{key}' not found in linkTable.");
                continue;
            }

            var linkNodeText = $"<a href='{url}'>{linkText}</a>";
            AddNode(currentNode, linkNodeText);
            usedKeys.Add(key);
            IEnumerable<string> collection = links.Where(x => key.Contains(x.Key)).Select(x => x.Key);
            foreach (var item in collection)
            {
                usedKeys.Add(item);
            }
        }

        if (lastPos < originalText.Length)
        {
            var afterNodeText = originalText.Substring(lastPos);
            AddNode(currentNode, afterNodeText);
        }
        //foreach (var item in matches)
        //{
        //    usedKeys.Add(item.Value);
        //}
        currentNode.ParentNode.RemoveChild(currentNode);
    }

    private void ProcessNode3(HtmlNode node)
    {
        string originalText = $"{HtmlEntity.DeEntitize(node.InnerText)}";
        Console.WriteLine($"Processing Node: {originalText}");

        var matches = trie.ParseText(originalText);
        if (!matches.Any()) return;

        var replacements = matches
            .OrderByDescending(x => x.Value.Length)
            .Select(m => (Start: m.Begin, End: m.End, Key: m.Value))
            .Where(match => !usedKeys.Contains(match.Key)).ToList();

        HtmlNode currentNode = node;
        int lastPos = 0;

        foreach (var (start, end, key) in replacements)
        {
            if (usedKeys.Contains(key))
                continue;

            IEnumerable<string> collection = links.Where(x => key.Contains(x.Key)).Select(x => x.Key);
            foreach (var item in collection)
            {
                usedKeys.Add(item);
            }

            if (start < lastPos)
                continue;

            string beforeText = originalText.Substring(lastPos, start - lastPos);
            string linkText = string.Empty;

            if (start < 0 || start >= originalText.Length)
            {
                Console.WriteLine($"Invalid index: start={start}, end={end}, length={originalText.Length}");
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
                lastPos = end;
            }

            if (!string.IsNullOrEmpty(beforeText))
            {
                AddNode(currentNode, beforeText);
            }

            if (!links.TryGetValue(key.Trim(), out string url))
            {
                Console.WriteLine($"Key '{key}' not found in linkTable.");
                continue;
            }

            var linkNodeText = $"<a href='{url}'>{linkText}</a>";
            AddNode(currentNode, linkNodeText);
        }

        if (lastPos < originalText.Length)
        {
            var afterNodeText = originalText.Substring(lastPos);
            AddNode(currentNode, afterNodeText);
        }

        currentNode.ParentNode.RemoveChild(currentNode);
    }


}


