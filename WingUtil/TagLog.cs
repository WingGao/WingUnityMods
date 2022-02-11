using System;

namespace WingUtil
{
    public class TagLog
    {
        private Action<String> logger;
        private String tag = "";
        private int index = 0;

        public TagLog(Action<String> logger, String tag)
        {
            this.logger = logger;
            this.tag = tag;
        }

        public void Mark(String note = "")
        {
            logger.Invoke("[TAG] " + tag + " " + index + " " + note);
            index++;
        }
    }
}