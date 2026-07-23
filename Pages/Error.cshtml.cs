using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FamilyHub.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
	private readonly ILogger<ErrorModel> _logger;

	public ErrorModel(ILogger<ErrorModel> logger)
	{
		_logger = logger;
	}

	public string RequestId { get; private set; } = string.Empty;
	public string? ExceptionType { get; private set; }
	public string? ExceptionMessage { get; private set; }
	public string? ExceptionDetails { get; private set; }
	public bool ShowDetails { get; private set; }

	public void OnGet()
	{
		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
		var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

		if (feature?.Error is not Exception exception)
		{
			_logger.LogError("Error page reached without an exception feature. RequestId: {RequestId}, Path: {Path}, User: {User}",
				RequestId, feature?.Path ?? HttpContext.Request.Path, User.Identity?.Name ?? "Anonymous");
			Console.WriteLine($"[UnhandledException] RequestId={RequestId}; Path={HttpContext.Request.Path}; User={User.Identity?.Name ?? "Anonymous"}; No exception feature was available.");
			return;
		}

		ExceptionType = exception.GetType().FullName;
		ExceptionMessage = exception.Message;
		ExceptionDetails = exception.ToString();
		ShowDetails = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

		_logger.LogError(exception,
			"Unhandled exception reached the error page. RequestId: {RequestId}, Path: {Path}, OriginalPath: {OriginalPath}, User: {User}, ExceptionType: {ExceptionType}, Message: {Message}",
			RequestId, HttpContext.Request.Path, feature.Path, User.Identity?.Name ?? "Anonymous", exception.GetType().FullName, exception.Message);
		Console.WriteLine($"[UnhandledException] RequestId={RequestId}; Path={feature.Path ?? HttpContext.Request.Path}; User={User.Identity?.Name ?? "Anonymous"}; ExceptionType={exception.GetType().FullName}; Message={exception.Message}{Environment.NewLine}{exception}");
	}
}