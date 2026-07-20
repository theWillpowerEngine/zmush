var refreshTimer = null

function handleResult(res) {
    Z.frameText = res.text
    for (var l of res.logs) {
        if (!l) continue;
        Z.logs.push(l)
        $("#log").append(l + "<br />")
    }
}

const Z = {
    alwaysEdit: parseInt("~~CanEditByDefault~~"),
    frameText: null,
    logs: [],

    commands: [],
    commandIdx: -1,

    post(url, data, callback) {
        $.post(url, data instanceof Object ? JSON.stringify(data) : data, (res) => {
            callback(1, res)
        }).fail((xhr, status, error) => {
            if (xhr.status == 403) {
                Cookies.remove("ZSession")
                location.reload()
                return
            }

            if (xhr.status == 0 && !error) {
                Cookies.remove("ZSession")
                location.reload()
                return
            }

            console.error("API Error! Status: " + xhr.status + ", Error: " + error);
            console.error("Response Text: " + xhr.responseText);

            callback(0, { status: xhr.status, statusMsg: error, error: xhr.responseText })
        })
    },

    startGame() {
        $("#authed").fadeIn()
        $("#inputBar").fadeIn(() => {
            Cookies.set("ZInGame", "Yes")
            Z.frame()
            $("#cmd").focus()
        });
    },

    setRefreshTimer(clearOnly) {
        if (refreshTimer) clearTimeout(refreshTimer);
        if (clearOnly) return;

        refreshTimer = setTimeout(() => {
            Z.post("/log", window.sessionId, (suc, res) => {
                if (suc) {
                    var eles = res.split("\n")
                    for (var i = 0; i < eles.length; i++) {
                        if (!eles[i]) continue;
                        var e = eles[i].trim()
                        if (e != "") {
                            Z.logs.push(e)
                            $("#log").append(e + "<br />")
                        }
                    }

                    var removed = false;
                    while (Z.logs.length > 20) {
                        Z.logs.shift()
                        removed = true;
                    }

                    if (removed)
                        $("#log").html(Z.logs.join("<br />") + "<br />")

                } else {
                    Z.logs.push("API Error:  " + res.error)
                    $("#log").append("API Error:  " + res.error + "<br />")
                }
                Z.setRefreshTimer()
            })

        }, 5000);
    },

    frame(force) {
        Z.setRefreshTimer(true)

        if (!Z.frameText || force) {
            Z.frameText = null
            Z.post("/frame", window.sessionId, (suc, res) => {
                if (suc) {
                    handleResult(res)
                    Z.frame()
                } else {
                    Z.logs.push("API Error:  " + res.error)
                    $("#log").append("API Error:  " + res.error + "<br />")
                }
            })
        } else {
            var log = $("#log").html()
            if (!log || log == "undefined") log = ""

            $("#authed").html(Z.frameText)

            var newLog = $("#log").html()
            $("#log").html(log + newLog)

            Z.setRefreshTimer()
        }
    },

    command(cmd) {
        Z.setRefreshTimer(true)

        Z.commands.push(cmd)
        while (Z.commands.length > 20) Z.commands.shift()
        Z.commandIdx = -1

        if (cmd.toLowerCase() == "clear") {
            $("#log").html("")
            Z.logs = []
            Z.frame(true)
            $("#cmd").val("").focus()
            return
        }

        Z.post("/command", { sessionId: window.sessionId, command: cmd }, (suc, res) => {
            if (suc) {
                handleResult(res)

                Z.frame()
                $("#cmd").val("")
                $("#cmd").focus()
            } else {
                Z.logs.push("API Error:  " + res.error)
                $("#log").append("API Error:  " + res.error + "<br />")
            }
            Z.setRefreshTimer()
        })
    },

    showCodeEditor() {
        $("#codeEditor").fadeIn(() => {
            $("#codeEditorText").focus()
        })
    },

    hideCodeEditor() {
        $("#codeEditor").fadeOut(() => {
            $("#cmd").focus()
        })
    }
}