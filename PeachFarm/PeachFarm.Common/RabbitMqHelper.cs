using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Net;
using System.Net.Sockets;

namespace PeachFarm.Common
{
	public class RabbitMqHelper
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		private IConnection connection;
		private IModel receiver;
		private IModel sender;
		private IModel declarer;

		private string hostName;
		int port;
		string userName;
		string password;
		bool ssl;

		private System.Timers.Timer listenTimer;
		private string listenQueue;

		private bool processOne = false;

		public RabbitMqHelper(string hostName, int port = -1, string userName = "guest", string password = "guest", bool ssl = false)
		{
			if (String.IsNullOrEmpty(hostName))
			{
				throw new ApplicationException("hostName may not be null.");
			}

			this.hostName = hostName;
			this.port = port;
			this.userName = userName;
			this.password = password;
			this.ssl = ssl;

			this.LocalIP = GetLocalIP(this.hostName).ToString();

			// TODO: Investigate a better strategy for dealing with connection failures.
			for (int i = 0; ; ++i)
			{
				try
				{
					OpenConnection();
					return;
				}
				catch (Exception)
				{
					if (i >= 9)
						throw;

					System.Threading.Thread.Sleep(1000);
				}
			}
		}

		public string LocalIP { get; private set; }

		public bool IsListening { get; private set; }

		#region Public Methods

		public void StartListener(string queue, double interval = 1000, bool purgeQueue = true, bool processOne = false)
		{
			if (String.IsNullOrEmpty(queue))
			{
				throw new ApplicationException("queue parameter cannot be empty");
			}

			listenQueue = queue;
			this.processOne = processOne;

			lock (declarer)
			{
				declarer.QueueDeclare(listenQueue, true, false, false, null);
				if (purgeQueue)
				{
					declarer.QueuePurge(listenQueue);
				}
			}
			listenTimer = new System.Timers.Timer(interval);
			listenTimer.Elapsed += Listen;
			listenTimer.Start();
			IsListening = true;
		}

		public void StopListener(bool deleteQueue = true)
		{
			IsListening = false;
			
			listenTimer.Stop();
			listenTimer.Dispose();
			listenTimer = null;

			if (deleteQueue)
			{
				lock (declarer)
				{
					declarer.QueueDelete(listenQueue);
				}
			}
			listenQueue = String.Empty;
		}

		public void ResumeListening()
		{
			IsListening = true;
		}

		public void PublishToQueue(string queue, string body, string action, string replyQueue = "")
		{
			RunGuarded(sender, () => { PublishToQueue(sender, queue, body, action, replyQueue); });

			#region old
			/*
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
						PublishToQueue(sender, queue, body, action, replyQueue);
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
			//*/
			#endregion
		}

		public void DeclareExchange(string exchange, List<string> queues, string routingKey = "")
		{
			RunGuarded(declarer, () =>
				{
					declarer.ExchangeDeclare(exchange, "fanout", true);
					foreach (var q in queues)
					{
						declarer.QueueBind(q, exchange, routingKey);
					}
				});

			#region old
			/*
			bool open = true;

			try
			{
				lock (declarer)
				{
					if (declarer.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						declarer.ExchangeDeclare(exchange, "fanout", true);
						foreach (var q in queues)
						{
							declarer.QueueBind(q, exchange, routingKey);
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
			//*/
			#endregion
		}

		public void BindQueueToExchange(string exchange, string queue, string routingKey = "")
		{
			RunGuarded(declarer, () =>
				{
					declarer.QueueBind(queue, exchange, routingKey);
				});

			#region old
			/*
			bool open = true;

			try
			{
				lock (declarer)
				{
					if (declarer.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						declarer.QueueBind(queue, exchange, routingKey);
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
			//*/
			#endregion
		}

		public void DeleteExchange(string exchange)
		{
			RunGuarded(declarer, () => { declarer.ExchangeDelete(exchange, true); });

			#region old
			/*
			bool open = true;

			try
			{
				lock (declarer)
				{
					if (declarer.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						declarer.ExchangeDelete(exchange, true);
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
			//*/
			#endregion
		}

		public virtual void PublishToExchange(string exchange, string body, string action)
		{
			RunGuarded(sender, () => { PublishToExchange(sender, exchange, body, action); });

			#region old
			/*
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
						PublishToExchange(sender, exchange, body, action);
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
			//*/
			#endregion
		}

		public void CloseConnection()
		{
			if (sender.IsOpen)
				sender.Close();

			if (receiver.IsOpen)
				receiver.Close();

			if (declarer.IsOpen)
				declarer.Close();

			if (connection.IsOpen)
				connection.Close();

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

		private void RunGuarded(IModel model, Action action)
		{
			bool open = true;

			try
			{
				lock (model)
				{
					if (model.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						action.Invoke();
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

		private T RunGuarded<T>(IModel model, Func<T> function)
		{
			bool open = true;

			try
			{
				lock (model)
				{
					if (model.IsOpen == false)
					{
						open = ReopenConnection();
					}

					if (open)
					{
						return function.Invoke();
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
						if (processOne)
						{
							IsListening = false;
						}

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
				} while (result != null && !processOne);
			}
		}

		protected virtual void OpenConnection()
		{
			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = hostName;
			factory.Port = port;
			factory.UserName = userName;
			factory.Password = password;
			factory.Ssl.Enabled = ssl;
			factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch;

			try
			{
				connection = factory.CreateConnection();
				sender = connection.CreateModel();
				receiver = connection.CreateModel();
				declarer = connection.CreateModel();
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

					if (declarer.IsOpen == false)
					{
						declarer = connection.CreateModel();
					}
				}
				catch
				{
					return false;
				}

			}

			return success;
		}

		private void PublishToQueue(IModel model, string queue, string message, string action, string replyQueue = "")
		{
			RabbitMQ.Client.Framing.v0_9_1.BasicProperties properties = new RabbitMQ.Client.Framing.v0_9_1.BasicProperties();
			properties.DeliveryMode = 2;
			properties.Headers = new Dictionary<string, string>();
			properties.Headers.Add("Action", action);
			if (String.IsNullOrEmpty(replyQueue) == false)
			{
				properties.Headers.Add("ReplyQueue", replyQueue);
			}
			model.BasicPublish("", queue, properties, encoding.GetBytes(message));
		}

		private void PublishToExchange(IModel model, string exchange, string message, string action)
		{
			RabbitMQ.Client.Framing.v0_9_1.BasicProperties properties = new RabbitMQ.Client.Framing.v0_9_1.BasicProperties();
			properties.DeliveryMode = 2;
			properties.Headers = new Dictionary<string, string>();
			properties.Headers.Add("Action", action);
			model.BasicPublish(exchange, "", properties, encoding.GetBytes(message));
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

		#region IP Helpers

		public static IPAddress GetLocalIP(string hostName)
		{
			using (var u = new UdpClient(hostName, 1))
			{
				var local = u.Client.LocalEndPoint as IPEndPoint;
				return local.Address;
			}
		}

		#endregion
	}

	public class RabbitMqException : Exception
	{
		public RabbitMqException(Exception innerException, string host = "", string message = "Peach Farm encountered a RabbitMq Exception.")
			: base(message, innerException)
		{
			this.RabbitMqHost = host;
		}

		public string RabbitMqHost { get; private set; }
	}


}
