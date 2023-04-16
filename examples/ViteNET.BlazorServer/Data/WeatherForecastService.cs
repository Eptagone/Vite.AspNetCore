// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace ViteNET.BlazorServer.Data;

public class WeatherForecastService
{
	private static readonly string[] Summaries = new[]
	{
	"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

	public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
	{
		return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
		{
			Date = startDate.AddDays(index),
			TemperatureC = Random.Shared.Next(-20, 55),
			Summary = Summaries[Random.Shared.Next(Summaries.Length)]
		}).ToArray());
	}
}