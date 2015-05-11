# AthWebClient
## Description

AthWebClient is another Http client library for .NET. It intended for using instead of [HttpWebRequest](https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest(v=vs.110).aspx) or [WebClient](https://msdn.microsoft.com/en-us/library/system.net.webclient(v=vs.110).aspx) classes.
Main difference from HttpWebRequest: **no magic**, until requested. By default, AthWebClient will make only requested actions. And nothing more.

### Magic of HttpWebRequest
* KeepAlive is on by default. As result, if you perform only single request, .NET will bring up lot of machinery and will take connection to remote server
* By default, only 2 connections to server allowed (with hidden internal queue)
* Very dumb inner queue with bad balancing
* Unusable timeouts: timeout for all request sound good, but there are differences for 100 sec for downloading 1000 bytes or 10 Gbytes
* Enabled WriteBuffering is on by default (it seems, without chunking, it will be anyway, but can be fixed)
* Min Chunk Size is 1024 byte. It impossible to send smaller data
* Server Certificate validation callback through static ServicePointManager
* Bad options for Ssl configuration
* Any 'error' response from server causes exception. E.g. 404 with a lot of useful data
* Own Dns-caching
* Expect-100-continue with unpredictable results (e.g. 350ms latency for posts)
* Problem with multiple headers with same name (e.g. Set-Cookie)
* Inability to set lot of headers directly, only through specific properties
* Inability to send body data with GET (yes, it sounds and looks strange, but why not?)
* Server redirect support by default is on

Due this situation, I decide to realize own dumb Http Client with simple, but configurable interface without additional magic by default.

## What Realized
* Chunking support for request/response
* HTTPS support with security configuration and connection information
* Support of automatic decompression of GZip/Defalte response (if requested)
* Timeouts for Connect, Send, Receive

## TODO
* KeepAlive suport
* Limit request number to server
* Proxy support (with autentication)
* Authentication support
* Timeouts configuration (better support)
* Request cancelling (better support)
* Api improvements (better support of standard headers)
* Automatic Redirects
* Expect 100 Continue
* AthWebClient (analog of WebClient with simple methods)
* .NET 4.5 Async/Await full support
