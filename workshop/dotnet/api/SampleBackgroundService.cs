namespace api;

public class SampleBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Background service started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Background service is running...");
                await Task.Delay(1000, stoppingToken); // Simulate work
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Background service canceled.");
        }
        finally
        {
            Console.WriteLine("Background service stopping.");
            // Cleanup logic
        }
    }
}
