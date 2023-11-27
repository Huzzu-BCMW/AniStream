﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using AndroidX.Core.App;
using AniStream.Utils.Downloading;
using Httpz.Hls;

namespace AniStream.Services;

public class ServiceHandler : Handler
{
    private readonly DownloadService _downloadService;
    public System.Threading.CancellationTokenSource CancellationTokenSource = new();

    public ServiceHandler(DownloadService service, Looper looper) : base(looper)
    {
        _downloadService = service;
    }

    public override async void HandleMessage(Message msg)
    {
        if (msg.Data is null)
            return;

        var downloader = new HlsDownloader(_downloadService);

        var stream = JsonSerializer.Deserialize<HlsStream>(msg.Data.GetString("stream")!)!;
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(msg.Data.GetString("headers")!)!;
        var fileName = msg.Data.GetString("fileName")!;

        try
        {
            await downloader.DownloadAsync(
                fileName,
                stream,
                headers,
                CancellationTokenSource.Token
            );
        }
        catch (Exception e)
        {
            //DownloadService.SendNotification(_downloadService, 4, "Error", $"{e}");
            Log.Debug("AniStream DownloadService", $"{e}");
        }

        base.HandleMessage(msg);
    }
}

[Service(Exported = true, Enabled = true, Name = "com.oneb.anistreamffmpeg.DownloadService")]
public class DownloadService : Service
{
    private Looper mServiceLooper = default!;
    private ServiceHandler mServiceHandler = default!;

    const string Tag = "AniStream DownloadService";

    public override void OnCreate()
    {
        base.OnCreate();

        var thread = new HandlerThread("ServiceStartArguments", (int)ThreadPriority.Background);
        thread.Start();

        mServiceLooper = thread.Looper!;
        mServiceHandler = new ServiceHandler(this, mServiceLooper);
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        base.OnTaskRemoved(rootIntent);

        Log.Debug(Tag, "Task removed");
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        if (intent is not null)
        {
            if (intent.Action == "cancel_download")
            {
                mServiceHandler?.CancellationTokenSource.Cancel();

                StopForeground(StopForegroundFlags.Remove);
                StopSelf();
                return StartCommandResult.Sticky;
            }

            Log.Debug(Tag, "Service started");

            var msg = mServiceHandler.ObtainMessage();
            msg.Arg1 = startId;//needed for stop.

            msg.Data = intent.Extras;

            mServiceHandler.SendMessage(msg);
        }

        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnDestroy()
    {
        Log.Debug(Tag, "Service destroyed");

        base.OnDestroy();
    }

    public override bool StopService(Intent? name)
    {
        Log.Debug(Tag, "Service stopped");

        return base.StopService(name);
    }

    public static void SendNotification(Service context, string textTitle, string textContent)
    {
        var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;

        var intent = new Intent();
        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, flags);

        var channelId = $"{context.PackageName}.general";

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(Resource.Drawable.logo_notification)
            .SetOngoing(true)
            .SetContentTitle(textTitle)
            .SetContentText(textContent)
            .SetContentIntent(pendingIntent)
            //.SetPriority(NotificationCompat.PriorityDefault);
            .SetPriority(NotificationCompat.PriorityLow);

        context.StartForeground(2387, builder.Build());
    }

    public static void SendNotification(
        Context context,
        int notificationId,
        string textTitle,
        string textContent)
    {
        var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;

        var intent = new Intent();
        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, flags);

        var channelId = $"{context.PackageName}.general";

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(Resource.Drawable.logo_notification)
            .SetOngoing(false)
            .SetContentTitle(textTitle)
            .SetContentText(textContent)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityLow);

        var notificationManager = NotificationManagerCompat.From(context);
        //var notificationManager = (NotificationManager)GetSystemService(NotificationService);

        notificationManager.Notify(notificationId, builder.Build());
    }
}