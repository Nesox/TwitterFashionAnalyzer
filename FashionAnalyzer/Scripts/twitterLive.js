$(function () {

    var twitterHub = $.connection.twitterHub;
    twitterHub.client.setTaskId = function (id) {
        $("#stopStream").attr("data-id", id);
    }

    $("#startStream").on("click", function () {
        twitterHub.server.startTwitterLive();
    });

    twitterHub.client.updateStatus = function (status) {
        $("#streamStatus").html(status);
    }

    twitterHub.client.updateTweet = function (tweet) {
        $(tweet.HTML)
            .hide()
            .prependTo(".tweets")
            .fadeIn("slow");
    };

    twitterHub.client.updateTweetHtml = function (html) {
        $(html)
            .hide()
            .prependTo(".tweets")
            .fadeIn("slow");
    };



    $("#stopStream").on("click", function () {
        var id = $(this).attr("data-id");
        twitterHub.server.stopTwitterLive(id);
    });

    $.connection.hub.start();
});