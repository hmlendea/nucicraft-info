(function() {
    "use strict";

    var playersElement = document.getElementById('server-players');
    if (!playersElement) {
        return;
    }

    fetch('/api/nucicraft/server')
        .then(function(response) { return response.ok ? response.json() : null; })
        .then(function(server) {
            if (!server || server.onlinePlayersCount === null || server.onlinePlayersCount === undefined) {
                return;
            }

            playersElement.textContent = server.onlinePlayersCount;
        })
        .catch(function() {});
})();
