using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PeachFarm.Common
{
  public static class QueueNames
  {
    public static string EXCHANGE_CONTROLLER = "";
    public static string QUEUE_CONTROLLER = "peachfarm.controller.{0}";

    public static string EXCHANGE_ADMIN = "";
    public static string QUEUE_ADMIN = "peachfarm.controller.{0}";

    public static string EXCHANGETYPE_NODE = "fanout";
    public static string EXCHANGE_NODE = "peachfarm.node";
    public static string QUEUE_NODE = "peachfarm.node.{0}";
  }
}
