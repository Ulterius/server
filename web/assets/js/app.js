if (!("WebSocket" in window)) {
    alert('Your browser does not support WebSockets');
}
var host = "ws://192.168.1.3:8387/";
var socket = new WebSocket(host);
function connect() {


    try {

        console.log('Socket Status: ' + socket.readyState);
        socket.onopen = function () {
            console.log('Socket Status: ' + socket.readyState + ' (open)');
			sendCommand();
        };
        socket.onmessage = function (e) {
            if (typeof e.data === "string") {
                var jsonData = JSON.parse(e.data);
               // console.log('String message received: ' + e.data);
                for (var i = 0; i < jsonData.length; i++) {
                    var obj = jsonData[i];
                    var id = obj.id;
                    var name = obj.name;
                    var path = obj.path;
                    var icon = obj.icon;
					if (icon === "null") {
                        icon = '/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAgACADASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDy3Q/hzr3iHSYtSsEie3kLAcOSCCQQcKR29a0f+FO+K/8AnjF/3xL/APEV658FnnX4c2Aia4C/apt3lFNv3v4t3OPpz+lZev6/q8PiDU449Wv0RLuZVVblwFAcgADPArswmEeJk0naxyYvFrDRTavc83/4U74r/wCeMX/fEv8A8RVDX/hp4g8N6RNqeorAtvEyqdrNkkttGMqB/wDW/CvePAWrareW2qNLf3s7I8IXfI0hUESZxlHx0HYdBzVH4xzXMvwwvvtDSti5g2+YpH8R6ZjT+v4d8sRQdCq6bd7GmHrqvSVRK1w+DFv53w5099sJ8u6mP7yLcR838JyMHj3rI8QaBrM3iPVJYtJv3je7lZHW2chgXJBBxyK8d0rxz4m0PT0sNM1ee1tUJKxxhcZJyT0q/wD8LU8c/wDQyXn5r/hWuExksNJtK9zLF4OOJiot2se7+A9D1GO21OO60+WAs8LKLqLYGwJM43owPUdu/X1q/GbT0sfhdd7cbpLiDcAiAAhj3VVz17/pXiX/AAtPxx/0Ml5/47/hVHVvHfifXdPew1TWJ7q1cgtHIFxkHIPSssRXdeq6jVrmuHoKhSVNO9j/2Q==';
                    }
					if (path === "null") {
						continue;
					}
                    var tbody = $('#taskBody');
                    var image = 'data:image/png;base64,' + icon;
                    tbody.append('<tr><td><img id="img" src="' + image + '" />' + name + '</td><td>' + id + '</td><td>' + path + '</td></tr>');
                    

                }
            } else if (e.data instanceof ArrayBuffer) {
                console.log('ArrayBuffer received: ' + e.data);
            } else if (e.data instanceof Blob) {
                console.log('Blob received: ' + e.data);
            }
        };
        socket.onclose = function (event) {
            var reason;
            console.log(event.code);
            // See http://tools.ietf.org/html/rfc6455#section-7.4.1
            if (event.code == 1000)
                reason = "Normal closure, meaning that the purpose for which the connection was established has been fulfilled.";
            else if (event.code == 1001)
                reason = "An endpoint is \"going away\", such as a server going down or a browser having navigated away from a page.";
            else if (event.code == 1002)
                reason = "An endpoint is terminating the connection due to a protocol error";
            else if (event.code == 1003)
                reason = "An endpoint is terminating the connection because it has received a type of data it cannot accept (e.g., an endpoint that understands only text data MAY send this if it receives a binary message).";
            else if (event.code == 1004)
                reason = "Reserved. The specific meaning might be defined in the future.";
            else if (event.code == 1005)
                reason = "No status code was actually present.";
            else if (event.code == 1006)
                reason = "The connection was closed abnormally, e.g., without sending or receiving a Close control frame";
            else if (event.code == 1007)
                reason = "An endpoint is terminating the connection because it has received data within a message that was not consistent with the type of the message (e.g., non-UTF-8 [http://tools.ietf.org/html/rfc3629] data within a text message).";
            else if (event.code == 1008)
                reason = "An endpoint is terminating the connection because it has received a message that \"violates its policy\". This reason is given either if there is no other sutible reason, or if there is a need to hide specific details about the policy.";
            else if (event.code == 1009)
                reason = "An endpoint is terminating the connection because it has received a message that is too big for it to process.";
            else if (event.code == 1010) // Note that this status code is not used by the server, because it can fail the WebSocket handshake instead.
                reason = "An endpoint (client) is terminating the connection because it has expected the server to negotiate one or more extension, but the server didn't return them in the response message of the WebSocket handshake. <br /> Specifically, the extensions that are needed are: " + event.reason;
            else if (event.code == 1011)
                reason = "A server is terminating the connection because it encountered an unexpected condition that prevented it from fulfilling the request.";
            else if (event.code == 1015)
                reason = "The connection was closed due to a failure to perform a TLS handshake (e.g., the server certificate can't be verified).";
            else
                reason = "Unknown reason";

            console.log(reason);
        };
    } catch (exception) {
        console.log('Error' + exception);
    }
}

function sendCommand() {
    var command = "app://showProcessList?format=JSOhN";
    try {
        socket.send(command);
    } catch (exception) {
        console.log(exception);
    }
}