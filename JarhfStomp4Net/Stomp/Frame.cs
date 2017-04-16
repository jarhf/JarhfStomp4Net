using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JarhfStomp4Net.Stomp
{
    /// <summary>
    /// 参考：https://github.com/jmesnil/stomp-websocket/blob/master/lib/stomp.js
    /// </summary>
    /// @author JHF
    /// @since 4.6
    public class Frame
    {
        /// <summary>
        /// 换行符，用于构造 Stomp 消息包
        /// </summary>
        public static Char LF = '\x0A';// Convert.ToChar(15);
        /// <summary>
        /// 空字符，用于构造 Stomp 消息包
        /// </summary>
        public static Char NULL = '\x00'; // Convert.ToChar(0);

        public CommandEnum Command { get; set; }
        public StompHeader Headers { get; set; } = new StompHeader();
        public string Body;

        public Frame()
        {
        }

        public Frame(CommandEnum command, StompHeader headers, string body)
        {
            this.Command = command;
            if (headers != null)
            {
                this.Headers = headers;
            }
            this.Body = body;
        }

        public override string ToString()
        {
            var lines = new List<string>() { this.Command.ToString() };
            bool skipContentLength = this.Headers.Get("content-length") == null ? true : false;
            if (skipContentLength)
            {
                this.Headers.Remove("content-length");
            }

            foreach (var header in this.Headers)
            {
                lines.Add(header.Key + ":" + header.Value);
            }
            if (this.Body != null && !skipContentLength)
            {
                lines.Add("content-length:" + (SizeOfUTF8(this.Body)));
            }
            lines.Add(LF.ToString() + this.Body);
            return string.Join(LF.ToString(), lines);
        }

        private int SizeOfUTF8(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            //stomp.js中用的是这种方法获取utf8长度，我改成C#中的GetByteCount()方法了，应该是一样的。
            //return Regex.Matches(Uri.EscapeUriString(s), "%..|.").Count;
            return Encoding.UTF8.GetByteCount(s);
        }

        public string GetHeader(string headerName)
        {
            return Headers.Get(headerName);
        }

        public static Frame UnmarshallSingle(string data)
        {
            int divider = Math.Max(data.IndexOf("" + LF + LF), 0);
            var headerLines = data.Substring(0, divider).Split(LF);
            CommandEnum command = (CommandEnum)Enum.Parse(typeof(CommandEnum), headerLines.First());
            StompHeader headers = new Stomp.StompHeader();

            for (int i = 1, len = headerLines.Length; i < len; i++)
            {
                string line = headerLines[i];
                int idx = line.IndexOf(':');
                headers[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
            }
            string body = "";
            int start = divider + 2;
            if (headers.ContainsKey("content-length"))
            {
                int utf8Len = int.Parse(headers["content-length"]);
                //注意，stomp是以utf8字节来算字符串长度的！！！所以下面截取body要用UTF8字节处理。  
                //但是stomp.js的js源码中是直接用data.substring(start, start + utf8Len)处理的，虽然js容错性好不会报错。不过一般utf8长度肯定大于字符长度，不会少截断，貌似直接截到结束也没什么关系？详细的得去研究Stomp协议body的格式
                body = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(data.Substring(start)), 0, utf8Len);
            }
            else
            {
                Char? chr = null;
                int i, j, len;
                for (i = j = start, len = data.Length; start <= len ? j < len : j > len; i = start <= len ? ++j : --j)
                {
                    chr = data[i];
                    if (chr == NULL)
                    {
                        break;
                    }
                    body += chr;
                }
            }
            return new Frame(command, headers, body);
        }

        public static FrameSet Unmarshall(string datas)
        {
            string[] strArrayframes = Regex.Split(datas, "" + NULL + LF + "*");

            FrameSet framesObj = new FrameSet();
            framesObj.Frames = strArrayframes.Take(strArrayframes.Length - 1).ToList().ConvertAll(i => UnmarshallSingle(i));

            string lastFrame = strArrayframes.Last();
            if (lastFrame == LF.ToString() || (Regex.Match(lastFrame, "" + NULL + LF + "*$")).Success)
            {
                framesObj.Frames.Add(UnmarshallSingle(lastFrame));
            }
            else
            {
                framesObj.Partial = lastFrame;
            }
            return framesObj;
        }

        public static string Marshall(CommandEnum command, StompHeader headers, string body)
        {
            var frame = new Frame(command, headers, body);
            return frame.ToString() + NULL;
        }
    }

    /* Frame javascript 源代码：
     Frame = (function () {
        var unmarshallSingle;

        function Frame(command, headers, body) {
            this.command = command;
            this.headers = headers != null ? headers : {};
            this.body = body != null ? body : '';
        }

        Frame.prototype.toString = function () {
            var lines, name, skipContentLength, value, _ref;
            lines = [this.command];
            skipContentLength = this.headers['content-length'] === false ? true : false;
            if (skipContentLength) {
                delete this.headers['content-length'];
            }
            _ref = this.headers;
            for (name in _ref) {
                if (!__hasProp.call(_ref, name)) continue;
                value = _ref[name];
                lines.push("" + name + ":" + value);
            }
            if (this.body && !skipContentLength) {
                lines.push("content-length:" + (Frame.sizeOfUTF8(this.body)));
            }
            lines.push(Byte.LF + this.body);
            return lines.join(Byte.LF);
        };

        Frame.sizeOfUTF8 = function (s) {
            if (s) {
                return encodeURI(s).match(/%..|./g).length;
            } else {
                return 0;
            }
        };

        unmarshallSingle = function (data) {
            var body, chr, command, divider, headerLines, headers, i, idx, len, line, start, trim, _i, _j, _len, _ref, _ref1;
            divider = data.search(RegExp("" + Byte.LF + Byte.LF));
            headerLines = data.substring(0, divider).split(Byte.LF);
            command = headerLines.shift();
            headers = {};
            trim = function (str) {
                return str.replace(/^\s+|\s+$/g, '');
            };
            _ref = headerLines.reverse();
            for (_i = 0, _len = _ref.length; _i < _len; _i++) {
                line = _ref[_i];
                idx = line.indexOf(':');
                headers[trim(line.substring(0, idx))] = trim(line.substring(idx + 1));
            }
            body = '';
            start = divider + 2;
            if (headers['content-length']) {
                len = parseInt(headers['content-length']);
                body = ('' + data).substring(start, start + len);
            } else {
                chr = null;
                for (i = _j = start, _ref1 = data.length; start <= _ref1 ? _j < _ref1 : _j > _ref1; i = start <= _ref1 ? ++_j : --_j) {
                    chr = data.charAt(i);
                    if (chr === Byte.NULL) {
                        break;
                    }
                    body += chr;
                }
            }
            return new Frame(command, headers, body);
        };

        Frame.unmarshall = function (datas) {
            var frame, frames, last_frame, r;
            frames = datas.split(RegExp("" + Byte.NULL + Byte.LF + "*"));
            r = {
                frames: [],
                partial: ''
            };
            r.frames = (function () {
                var _i, _len, _ref, _results;
                _ref = frames.slice(0, -1);
                _results = [];
                for (_i = 0, _len = _ref.length; _i < _len; _i++) {
                    frame = _ref[_i];
                    _results.push(unmarshallSingle(frame));
                }
                return _results;
            })();
            last_frame = frames.slice(-1)[0];
            if (last_frame === Byte.LF || (last_frame.search(RegExp("" + Byte.NULL + Byte.LF + "*$"))) !== -1) {
                r.frames.push(unmarshallSingle(last_frame));
            } else {
                r.partial = last_frame;
            }
            return r;
        };

        Frame.marshall = function (command, headers, body) {
            var frame;
            frame = new Frame(command, headers, body);
            return frame.toString() + Byte.NULL;
        };

        return Frame;

    })();
     */


    public class FrameSet
    {
        public List<Frame> Frames { get; set; } = new List<Frame>();

        public string Partial { get; set; }
    }
}
