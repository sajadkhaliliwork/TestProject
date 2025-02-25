using HtmlConvert.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.Controllers;

[ApiController]
[Route("[controller]")]
public class HtmlConvertController : ControllerBase
{
    private readonly IConvertorHtml convertHtml;

    public HtmlConvertController(IConvertorHtml convertHtml)
    {
        this.convertHtml = convertHtml;
    }

    [HttpGet]
    public string Get()
    {
        var file = System.IO.File.ReadAllText("Topic.html");

        return convertHtml.Convert(file);
    }

    [HttpGet( "Convert/{input}")]
    public string Convert(string input)
    {
        return convertHtml.Convert(input);
    }
}





