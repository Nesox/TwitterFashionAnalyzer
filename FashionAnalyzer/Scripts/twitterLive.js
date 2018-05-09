$(function () {
    var twitterHub = $.connection.twitterHub;

    // Function defintions
    twitterHub.client.setTaskId = function (id) {
        $("#stopStream").attr("data-id", id);
    }

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

    // Click handlers
    $("#startStream").on("click", function () {
        var filter = $('#inpHashtagFilter').val();
        twitterHub.server.startTwitterLive(filter);
    });

    $("#stopStream").on("click", function () {
        var id = $(this).attr("data-id");
        twitterHub.server.stopTwitterLive(id);
    });

    // Other events
    $("body").on('DOMSubtreeModified', "#tweet-container", function () {

        var numItems = $(".tweet-item").length;
        if (numItems > 10) {
            $('#tweet-container .tweet-item:last').fadeOut('slow',
                function() {
                    $(this).remove();
                });
        }
    });

    /*
    // Changing filters on the fly. todo: not working yet :/
    var typingTimer;
    var doneTypingInterval = 5000; //time in ms, 5 second for example
    var $input = $('#inpHashtagFilter');

    // on keyup, start the countdown.
    $input.on('keyup',
        function() {
            clearTimeout(typingTimer);
            typingTimer = setTimeout(doneTyping, doneTypingInterval);
        });

    //on keydown, clear the countdown 
    $input.on('keydown',
        function() {
            clearTimeout(typingTimer);
        });

    //user is "finished typing," do something
    function doneTyping() {
        var filter = $('#inpHashtagFilter').val();

        // Stop the stream first.
        var id = $(this).attr("data-id");
        twitterHub.server.stopTwitterLive(id);

        // restart the stream.
        twitterHub.startTwitterLive(filter);
    }*/

    $.connection.hub.start();
});