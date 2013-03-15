using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using System.ComponentModel;
using System.Diagnostics;

namespace PeachFarm.Common
{
	public class RabbitMqHelper
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		private static IConnection connection;
		private static IModel receiver;
		private static IModel sender;

		private string hostName;
		int port;
		string userName;
		string password;

		private static System.Timers.Timer listenTimer;
		private static string listenQueue;

		public RabbitMqHelper(string hostName, int port = -1, string userName = "guest", string password = "guest")
		{
			if (String.IsNullOrEmpty(hostName))
			{
				throw new ApplicationException("hostName may not be null.");
			}

			this.hostName = hostName;
			this.port = port;
			this.userName = userName;
			this.password = password;

			OpenConnection();
		}

		public bool IsListening { get; private set; }

		#region Public Methods

		public void StartListener(string queue, double interval = 1000)
		{
			if (String.IsNullOrEmpty(queue))
			{
				throw new ApplicationException("queue parameter cannot be empty");
			}
			
			IsListening = true;

			listenQueue = queue;
			lock (sender)
			{
				sender.QueueDeclare(listenQueue, true, false, false, null);
				sender.QueuePurge(listenQueue);
			}
			listenTimer = new System.Timers.Timer(interval);
			listenTimer.Elapsed += Listen;
			listenTimer.Start();
		}

		public void StopListener()
		{
			IsListening = false;

			listenTimer.Stop();
			listenTimer = null;
			lock (sender)
			{
				sender.QueueDelete(listenQueue);
			}
			listenQueue = String.Empty;
		}

		public void PublishToQueue(string queue, string body, string action, string replyQueue = "")
		{
			bool open = true;

			try
			{
				lock (sender)
				{
					if (sender.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						sender.QueueDeclare(queue, true, false, false, null);
						sender.PublishToQueue(queue, body, action, replyQueue);
					}
					else
					{
						throw new RabbitMqException(null, hostName);
					}
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		public void DeclareExchange(string exchange, List<string> queues, string routingKey = "")
		{
			bool open = true;

			try
			{
				lock (sender)
				{
					if (sender.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						sender.ExchangeDeclare(exchange, "fanout", true);
						foreach (var q in queues)
						{
							sender.QueueBind(q, exchange, routingKey);
						}
					}
					else
					{
						throw new RabbitMqException(null, hostName);
					}
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		public void BindQueueToExchange(string exchange, string queue, string routingKey = "")
		{
			bool open = true;

			try
			{
				lock (sender)
				{
					if (sender.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						sender.QueueBind(queue, exchange, routingKey);
					}
					else
					{
						throw new RabbitMqException(null, hostName);
					}
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		public void DeleteExchange(string exchange)
		{
			bool open = true;

			try
			{
				lock (sender)
				{
					if (sender.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						sender.ExchangeDelete(exchange, true);
					}
					else
					{
						throw new RabbitMqException(null, hostName);
					}
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		public void PublishToExchange(string exchange, string body, string action)
		{
			bool open = true;

			try
			{
				lock (sender)
				{
					if (sender.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						sender.PublishToExchange(exchange, body, action);
					}
					else
					{
						throw new RabbitMqException(null, hostName);
					}
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		#endregion

		#region MessageReceived
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		private void OnMessageReceived(string action, string body, string replyQueue)
		{
			if (MessageReceived != null)
			{
				MessageReceived(this, new MessageReceivedEventArgs(action, body, replyQueue));
			}
		}

		public class MessageReceivedEventArgs : EventArgs
		{
			public MessageReceivedEventArgs(string action, string body, string replyQueue)
			{
				this.Action = action;
				this.Body = body;
				this.ReplyQueue = replyQueue;
			}

			public string ReplyQueue { get; private set; }
			public string Action { get; private set; }
			public string Body { get; private set; }
		}
		#endregion

		#region private functions

		private void Listen(object source, System.Timers.ElapsedEventArgs e)
		{
			if (IsListening)
			{
				BasicGetResult result = null;
				do
				{
					result = null;

					try
					{
						result = GetFromQueue(listenQueue);
					}
					catch (RabbitMqException rex)
					{
						Debug.WriteLine(rex.InnerException.ToString());
					}

					if (result != null)
					{
						AckMessage(result.DeliveryTag);

						string body = encoding.GetString(result.Body);
						string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
						string replyQueue = String.Empty;
						if (result.BasicProperties.Headers.Contains("ReplyQueue"))
						{
							replyQueue = encoding.GetString((byte[])result.BasicProperties.Headers["ReplyQueue"]);
						}

						OnMessageReceived(action, body, replyQueue);
					}
				} while (result != null);
			}
		}

		private void OpenConnection()
		{
			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = hostName;
			factory.Port = port;
			factory.UserName = userName;
			factory.Password = password;

			try
			{
				connection = factory.CreateConnection();
				sender = connection.CreateModel();
				receiver = connection.CreateModel();
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}

		}

		private bool ReopenConnection()
		{
			bool success = true;

			lock (connection)
			{
				if (connection.IsOpen == true)
				{
					try
					{
						CloseConnection();
					}
					catch { }
				}

				if (connection.IsOpen == false)
				{
					try
					{
						OpenConnection();
					}
					catch
					{
						return false;
					}
				}

				try
				{
					if (receiver.IsOpen == false)
					{
						receiver = connection.CreateModel();
					}

					if (sender.IsOpen == false)
					{
						sender = connection.CreateModel();
					}
				}
				catch
				{
					return false;
				}

			}

			return success;
		}

		private void CloseConnection()
		{
			if (sender.IsOpen)
				sender.Close();

			if (receiver.IsOpen)
				receiver.Close();

			if (connection.IsOpen)
				connection.Close();
		}

		private BasicGetResult GetFromQueue(string queue)
		{
			if (String.IsNullOrEmpty(queue))
			{
				return null;
			}

			bool open = true;
			try
			{
				if ((connection == null) || (connection.IsOpen == false))
				{
					open = ReopenConnection();
					if (open)
					{
						receiver.QueueDeclare(queue, true, false, false, null);
					}
				}

				if (open)
				{
					return receiver.BasicGet(queue, false);
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		private void AckMessage(ulong deliveryTag)
		{
			try
			{
				if (receiver.IsOpen == false)
					ReopenConnection();
				receiver.BasicAck(deliveryTag, false);
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex, hostName);
			}
		}

		#endregion
	}

	public class RabbitMqException : Exception
	{
		public RabbitMqException(Exception innerException, string host = "", string message = "Peach Farm Node encountered a RabbitMq Exception.")
			: base(message, innerException)
		{
			this.RabbitMqHost = host;
		}

		public string RabbitMqHost { get; private set; }
	}

	public static class RabbitMqExtensions
	{
		private static UTF8Encoding encoding = new UTF8Encoding();

		public static void PublishToQueue(this IModel model, string queue, string message, string action, string replyQueue = "")
		{
			RabbitMQ.Client.Framing.v0_9_1.BasicProperties properties = new RabbitMQ.Client.Framing.v0_9_1.BasicProperties();
			properties.Headers = new Dictionary<string, string>();
			properties.Headers.Add("Action", action);
			if (String.IsNullOrEmpty(replyQueue) == false)
			{
				properties.Headers.Add("ReplyQueue", replyQueue);
			}
			model.BasicPublish("", queue, properties, encoding.GetBytes(message));
		}

		public static void PublishToExchange(this IModel model, string exchange, string message, string action)
		{
			RabbitMQ.Client.Framing.v0_9_1.BasicProperties properties = new RabbitMQ.Client.Framing.v0_9_1.BasicProperties();
			properties.Headers = new Dictionary<string, string>();
			properties.Headers.Add("Action", action);
			model.BasicPublish(exchange, "", properties, encoding.GetBytes(message));
		}
	}

}
