        var timeOnPageLoad = new Date();
        var SessionTimeoutTimer = null;
        var sessionWarningTimer = null;
        //For warning
        if(parseInt(sessionTimeoutWarning) > 0)
            var sessionWarningTimer = setTimeout('SessionWarning()',
				    parseInt(sessionTimeoutWarning) * 60 * 1000);
            var sessionTimeoutTimer = setTimeout('SessionTimeout()',
                    parseInt(sessionTimeout) * 60 * 1000);

        //Session Warning
        function SessionWarning() {
            //minutes left for expiry
            var minutesForExpiry =  (parseInt(sessionTimeout) - 
					parseInt(sessionTimeoutWarning));
            var message = "Your session will expire in " + 
		minutesForExpiry + " mins. Do you want to extend the session?";

            //Confirm the user if he wants to extend the session
            answer = confirm(message);

            //if yes, extend the session.
            if (answer) {
                var img = new Image(1, 1);
                img.src = '/Misc/KeepAlive.aspx?date=' + escape(new Date());

                //reset the time on page load
                window.clearTimeout(sessionTimeoutTimer);
                window.clearTimeout(sessionWarningTimer);
                sessionWarningTimer = setTimeout('SessionWarning()',
				    parseInt(sessionTimeoutWarning) * 60 * 1000);
                sessionTimeoutTimer = setTimeout('SessionTimeout()',
                    parseInt(sessionTimeout) * 60 * 1000);
                return;
            }
        }

        function SessionTimeout() {
            alert("Session expired. Please save your data outside the browser and log in again.");
        }