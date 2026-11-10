// Carries the save file between the game and the player's YouTube account.
//
// A Playable has no disk: only what is handed to ytgame.game.saveData survives the tab, and
// certification fails a game that keeps progress only in the browser. Both calls answer through
// SendMessage to the GameObject 'Playables' (component CloudSave.Receiver, Assets/_Web/CloudSave.cs).
mergeInto(LibraryManager.library, {

    // Always answers exactly once: with the stored string, or with '' meaning "nothing up there".
    MicroCloudLoad: function () {
        var settled = false;
        var answer = function (text) {
            if (settled) return;
            settled = true;
            try {
                SendMessage('Playables', 'OnCloudLoaded', text || '');
            } catch (e) {
            }
        };

        if (typeof ytgame === 'undefined' || !ytgame.game || !ytgame.game.loadData) {
            answer('');
            return;
        }

        // A promise that never settles would hold the loading screen for ever.
        setTimeout(function () { answer(''); }, 3000);

        try {
            ytgame.game.loadData().then(function (text) {
                answer(typeof text === 'string' ? text : '');
            }, function () {
                answer('');
            });
        } catch (e) {
            answer('');
        }
    },

    // Fire and forget: the same data is already in PlayerPrefs on this device.
    MicroCloudSave: function (ptr) {
        if (typeof ytgame === 'undefined' || !ytgame.game || !ytgame.game.saveData) {
            return;
        }

        try {
            var promise = ytgame.game.saveData(UTF8ToString(ptr));
            // An unhandled rejection is a console error, and the certification preview reads the console.
            if (promise && promise.then) {
                promise.then(function () {}, function () {});
            }
        } catch (e) {
        }
    }
});
