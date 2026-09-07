using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AiAlreadyDidIt.Tests;

/// <summary>
/// "{action}" / "{controller}" inside an attribute route are MVC's reserved tokens: the segment then only matches the
/// literal action-method name, so a "{id}/{action}" verb route silently 404s for every real verb (admin apps grid, 2026-09-08).
/// </summary>
public class RouteTemplateTests
{
    [Fact]
    public void No_attribute_route_uses_reserved_action_or_controller_parameters()
    {
        var assembly = typeof(AiAlreadyDidIt.Api.Infrastructure.TextUtil).Assembly;
        var offenders = new List<string>();
        foreach (var type in assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var templates = type.GetCustomAttributes<RouteAttribute>().Select(r => r.Template)
                .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Select(a => a.Template)));
            foreach (var t in templates)
                if (t is not null && (t.Contains("{action", StringComparison.OrdinalIgnoreCase) || t.Contains("{controller", StringComparison.OrdinalIgnoreCase)))
                    offenders.Add($"{type.Name}: {t}");
        }
        Assert.Empty(offenders);
    }
}
