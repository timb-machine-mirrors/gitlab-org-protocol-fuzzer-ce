using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PeachFarm.Common
{
  public static class QueueNames
  {
    public static readonly string EXCHANGE_CONTROLLER = "";
    public static readonly string QUEUE_CONTROLLER = "peachfarm.controller.{0}";

    public static readonly string EXCHANGE_ADMIN = "";
    public static readonly string QUEUE_ADMIN = "peachfarm.admin.{0}";

    public static readonly string EXCHANGETYPE_NODE = "fanout";
    public static readonly string EXCHANGE_NODE = "peachfarm.node";
    public static readonly string QUEUE_NODE = "peachfarm.node.{0}";

    public static readonly string EXCHANGE_JOB = "peachfarm.job.{0}";
  }
}
