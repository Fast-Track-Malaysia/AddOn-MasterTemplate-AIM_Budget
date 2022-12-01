using System;

namespace FT_ADDON
{
    class ActionResult
    {
        public string status { get; set; }
        public string key { get; set; }
        public string reason { get; set; }

        public ActionResult(string status, string key, string reason)
        {
            this.status = status;
            this.key = key;
            this.reason = reason;
        }
    }
}
