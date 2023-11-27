﻿using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;

namespace AniStream.Adapters;

public class ServerWithVideos
{
    public bool IsLoaded { get; set; }

    public VideoServer VideoServer { get; set; }

    public List<VideoSource> Videos { get; set; }

    public ServerWithVideos(
        VideoServer videoServer,
        List<VideoSource> videos)
    {
        VideoServer = videoServer;
        Videos = videos;
    }
}

public class ExtractorAdapter : RecyclerView.Adapter
{
    private readonly Activity _activity;
    private readonly IAnimeInfo _anime;
    private readonly Episode _episode;

    public List<ServerWithVideos> Containers { get; set; }

    public ExtractorAdapter(
        Activity activity,
        IAnimeInfo anime,
        Episode episode,
        List<ServerWithVideos> containers)
    {
        _activity = activity;
        _anime = anime;
        _episode = episode;
        Containers = containers;
    }

    private class StreamViewHolder : RecyclerView.ViewHolder
    {
        public TextView streamName = default!;
        public RecyclerView streamRecyclerView = default!;

        public StreamViewHolder(View view) : base(view)
        {
            streamName = view.FindViewById<TextView>(Resource.Id.streamName)!;
            streamRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.streamRecyclerView)!;
        }
    }

    public override int ItemCount => Containers.Count;

    public override long GetItemId(int position) => position;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var streamViewHolder = (holder as StreamViewHolder)!;

        var server = Containers[streamViewHolder.BindingAdapterPosition].VideoServer;
        var videos = Containers[streamViewHolder.BindingAdapterPosition].Videos;

        streamViewHolder.streamName.Text = server.Name;

        if (streamViewHolder.streamRecyclerView.GetAdapter() is not VideoAdapter)
        {
            var adapter = new VideoAdapter(_activity, _anime, _episode, server, videos);

            streamViewHolder.streamRecyclerView.SetLayoutManager(new LinearLayoutManager(_activity));
            streamViewHolder.streamRecyclerView.HasFixedSize = true;
            streamViewHolder.streamRecyclerView.SetItemViewCacheSize(20);
            streamViewHolder.streamRecyclerView.SetAdapter(adapter);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var itemView = LayoutInflater.From(parent.Context)!
            .Inflate(Resource.Layout.item_stream, parent, false)!;

        return new StreamViewHolder(itemView);
    }
}