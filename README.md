# server-maintenance

This repo stores an application that is used to perform some system maintenance on servers I maintain.

Currenly all it does is backup up SQL Server databases to a folder and sends an email message. It was built because I have SQL Express running on a server and need to perform incremental backups so that they can be stored off-site. 

Unfortunately, SQL Express does not have the built in mainenance plan stuff that regular SQL Server has. So as a concequence, I had to figure out how to automatically run a backup programmatically. This repo is the result.

Once configured and compiled, I just run the executable on an interval using Scheduled Tasks with a service account that has access to the backup folder and the SQL Server.

After running the backups, the script runs a retention policy that deletes files older than 5-days so that the server doesn't fill up with old backups over time.

Then lastly, the script sends me an email to let me know how things went. If an error occurs, an email is also sent with a summary of what happened and the stack trace.

I may add onto this someday, but this is currently all it does.

Sorry for the lack of documentation... That will be coming soon (or not - depends on who needs it)...
