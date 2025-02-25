using HtmlConvert.Contracts;
using HtmlConvert.HtmlConvert.Implementing;
using HtmlConvert.Implementing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HtmlConvert;

public static class HostingExtentions
{
    public static IServiceCollection AddHtmlConvertService(this IServiceCollection services, IConfiguration configuration)
    {


        services.Configure<ExcelLinkOptions>(configuration.GetSection(key: nameof(ExcelLinkOptions)));

        services.AddTransient<ILinkProvider, ExcelLinkProvider>();
       // services.AddTransient<ILinkProvider>(sp => new ExcelLinkProvider("Link.xlsx"));
        services.AddTransient<IConvertorHtml, AhoCorasick2>();
        return services;
    }
}