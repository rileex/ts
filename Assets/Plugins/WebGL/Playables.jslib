// Everything the game says to YouTube Playables, and everything YouTube says back.
// Ported from Car Simulator Open City Stunts (passed McPlay pre-flight + YouTube certification).
//
// firstFrameReady is sent by index.html as soon as our loading screen is painted; gameReady is
// sent from here by the game when the garage is up and the player can actually play.
//
// Pause comes ONLY from ytgame.system.onPause/onResume - never the Page Visibility API, which
// certification forbids. Replies go back through SendMessage to the GameObject 'Playables'
// (see Assets/_Web/Playables.cs).
mergeInto(LibraryManager.library, {

    MicroPlayableAvailable: function () {
        return (typeof ytgame !== 'undefined' && ytgame && ytgame.game && ytgame.system) ? 1 : 0;
    },

    MicroPlayableReady: function () {
        if (typeof ytgame === 'undefined' || !ytgame.game || !ytgame.game.gameReady) {
            return;
        }

        try {
            ytgame.game.gameReady();
        } catch (e) {
            // Called twice or outside a Playable: not worth a console error (the preview reads it).
        }
    },

    MicroPlayableWatch: function () {
        if (typeof ytgame === 'undefined' || !ytgame.system) {
            return;
        }

        var say = function (method, value) {
            try {
                SendMessage('Playables', method, value ? '1' : '0');
            } catch (e) {
            }
        };

        // Holding Time.timeScale at zero freezes the picture but the engine keeps rendering it.
        // Reviewers check that the game is really inactive while paused, so the main loop is
        // stopped too: message first (the game gets a frame to freeze and save), then the loop.
        // On resume the loop comes back first, then the game is told.
        var loop = function (running) {
            window.__microHostPaused = !running;
            try {
                if (running) {
                    Module.resumeMainLoop();
                } else {
                    Module.pauseMainLoop();
                }
            } catch (e) {
            }
        };

        try {
            ytgame.system.onPause(function () {
                say('OnHostPause', true);
                loop(false);
            });

            ytgame.system.onResume(function () {
                loop(true);
                say('OnHostPause', false);

                // A mute that happened while paused arrives as no callback at all.
                try {
                    say('OnHostAudio', ytgame.system.isAudioEnabled());
                } catch (e) {
                }
            });
        } catch (e) {
        }

        try {
            ytgame.system.onAudioEnabledChange(function (enabled) { say('OnHostAudio', enabled); });
            // The state before the first change: a tab muted before the game opened gets no callback.
            say('OnHostAudio', ytgame.system.isAudioEnabled());
        } catch (e) {
        }
    },

    // Both ad calls answer exactly once, including when there was no ad. The ticket is handed
    // straight back so a late answer can't be taken for the answer to the next request.
    MicroPlayableInterstitial: function (ticket) {
        var settled = false;
        var done = function () {
            if (settled) return;
            settled = true;
            try {
                SendMessage('Playables', 'OnInterstitialFinished', '' + ticket);
            } catch (e) {
            }
        };

        if (typeof ytgame === 'undefined' || !ytgame.ads || !ytgame.ads.requestInterstitialAd) {
            done();
            return;
        }

        try {
            ytgame.ads.requestInterstitialAd().then(done, done);
        } catch (e) {
            done();
        }
    },

    MicroPlayableRewarded: function (rewardIdPtr, ticket) {
        var settled = false;
        var done = function (earned) {
            if (settled) return;
            settled = true;
            try {
                SendMessage('Playables', 'OnRewardedFinished', ticket + (earned ? ':1' : ':0'));
            } catch (e) {
            }
        };

        if (typeof ytgame === 'undefined' || !ytgame.ads || !ytgame.ads.requestRewardedAd) {
            done(false);
            return;
        }

        try {
            // The reward id is required and names the reward, never the player.
            ytgame.ads.requestRewardedAd(UTF8ToString(rewardIdPtr)).then(
                function (earned) { done(!!earned); },
                function () { done(false); }
            );
        } catch (e) {
            done(false);
        }
    }
});
