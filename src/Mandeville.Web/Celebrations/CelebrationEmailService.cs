namespace Mandeville.Web.Celebrations;

/// <summary>
/// Background job: from the configured hour each day it emails today's birthday and anniversary cards to the
/// celebrants and records them as sent. If the app was asleep at that hour it catches up when it wakes.
/// </summary>
public class CelebrationEmailService(IServiceScopeFactory scopeFactory, CelebrationMailer mailer, CelebrationEmailStatus status, ILogger<CelebrationEmailService> logger) : BackgroundService
{
    #region Private Fields

    private readonly IServiceScopeFactory __scopeFactory = scopeFactory;
    private readonly CelebrationMailer __mailer = mailer;
    private readonly CelebrationEmailStatus __status = status;
    private readonly ILogger<CelebrationEmailService> __logger = logger;

    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to finish starting, then look every 10 minutes.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (__mailer.IsConfigured)
                {
                    // Cards queued by an admin go first (their own image and message); then the plain automatic cards
                    //await RunQueueAsync(stoppingToken);

                    //if (__mailer.AutoEmail && AppClock.Now.Hour >= __mailer.SendHour)
                    //    await RunOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                __logger.LogError(ex, "The celebration email run failed.");
            }

            try { await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Emails the queued cards that are due: those whose celebration day is today (from the configured hour) or already past.
    /// A card that fails is marked Failed with the reason, so it is not retried every 10 minutes; the admin can retry it from the Celebrations tab.
    /// </summary>
    //private async Task RunQueueAsync(CancellationToken cancellationToken)
    //{
    //    using var scope = __scopeFactory.CreateScope();
    //    var repo = scope.ServiceProvider.GetRequiredService<ICelebrationRepository>();
    //    var photos = scope.ServiceProvider.GetRequiredService<IMemberPhotoRepository>();

    //    var now = AppClock.Now;
    //    var queued = await repo.SelectQueue("Queued", true, cancellationToken);
    //    var due = queued.Where(q => q.fdtmDueDate.Date < now.Date || (q.fdtmDueDate.Date == now.Date && now.Hour >= __mailer.SendHour));

    //    foreach (var item in due)
    //    {
    //        var card = item.ToCelebration();
    //        var photo = await photos.SelectForCard(card.MemberKey, card.SpouseKey, cancellationToken);
    //        var error = await __mailer.SendAsync(card, photo, item.fstrMessage, item.fbinImage, item.fstrImageContentType, cancellationToken);
    //        if (error is not null)
    //        {
    //            await repo.SetQueueStatus(item.flngKey, "Failed", error, "System", cancellationToken);
    //            continue;
    //        }

    //        await repo.SetQueueStatus(item.flngKey, "Sent", null, "System", cancellationToken);
    //        await repo.MarkSent(card, item.fstrMessage, "Email", string.Join(", ", card.Emails), "System", cancellationToken);
    //    }
    //}

    /// <summary>Emails every card due today that has not been sent and has not already failed today.</summary>
    //private async Task RunOnceAsync(CancellationToken cancellationToken)
    //{
    //    using var scope = __scopeFactory.CreateScope();
    //    var repo = scope.ServiceProvider.GetRequiredService<ICelebrationRepository>();
    //    var photos = scope.ServiceProvider.GetRequiredService<IMemberPhotoRepository>();

    //    var due = await repo.SelectUpcoming(0, cancellationToken);
    //    foreach (var card in due.Where(c => !c.Sent && c.QueueStatus is null && c.Emails.Count > 0 && __status.FailureFor(c.Id) is null))
    //    {
    //        var photo = await photos.SelectForCard(card.MemberKey, card.SpouseKey, cancellationToken);
    //        var error = await __mailer.SendAsync(card, photo, cancellationToken: cancellationToken);
    //        if (error is not null)
    //        {
    //            __status.RecordFailure(card.Id, error);
    //            continue;
    //        }

    //        await repo.MarkSent(card, null, "Email", string.Join(", ", card.Emails), "System", cancellationToken);
    //    }
    //}
}
