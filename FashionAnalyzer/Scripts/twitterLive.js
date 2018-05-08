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

    $("body").on('DOMSubtreeModified', "#tweet-container", function () {

        var numItems = $(".tweet-item").length;
        if (numItems > 10) {
            $('#tweet-container .tweet-item:last').fadeOut('slow',
                function() {
                    $(this).remove();
                });
        }
    });

    twitterHub.client.updateStatus = function (status) {
        $("#streamStatus").html(status);
    }

    twitterHub.client.updateStreamStats = function (statsString) {
        $("#streamStats").text(statsString);
    };

    twitterHub.client.updateTweetHtml = function (html) {
        $(html)
            .hide()
            .prependTo(".tweets")
            .fadeIn("slow");
    };


    $.connection.hub.start();
});