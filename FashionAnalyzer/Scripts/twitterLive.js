$(function () {

    var twitterHub = $.connection.twitterHub;
    twitterHub.client.setTaskId = function (id) {
        $("#stopStream").attr("data-id", id);
    }

    $("#btnUpdateFilter").on("click",
        function() {
            var filter = $('#inpHashtagFilter').val();
            alert(filter);
            twitterHub.server.updateFilters(filter);
        });
    
    $("#startStream").on("click", function () {
        twitterHub.server.startTwitterLive();
    });

    $("#stopStream").on("click", function () {
        var id = $(this).attr("data-id");
        twitterHub.server.stopTwitterLive(id);
    });

    twitterHub.client.updateStatus = function (status) {
        $("#streamStatus").html(status);
    }

    twitterHub.client.updateTweetHtml = function (html) {
        $(html)
            .hide()
            .prependTo(".tweets")
            .fadeIn("slow");
    };

    $.connection.hub.start();
});