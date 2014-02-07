//
// Connection.h
//
// $Id$
//
// Library: MongoDB
// Package: MongoDB
// Module:  Connection
//
// Definition of the Connection class.
//
// Copyright (c) 2012, Applied Informatics Software Engineering GmbH.
// and Contributors.
//
// Permission is hereby granted, free of charge, to any person or organization
// obtaining a copy of the software and accompanying documentation covered by
// this license (the "Software") to use, reproduce, display, distribute,
// execute, and transmit the Software, and to prepare derivative works of the
// Software, and to permit third-parties to whom the Software is furnished to
// do so, all subject to the following:
//
// The copyright notices in the Software and this entire statement, including
// the above license grant, this restriction and the following disclaimer,
// must be included in all copies of the Software, in whole or in part, and
// all derivative works of the Software, unless such copies or derivative
// works are solely in the form of machine-executable object code generated by
// a source language processor.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
// SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
// FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


#ifndef MongoDB_Connection_INCLUDED
#define MongoDB_Connection_INCLUDED


#include "Poco/Net/SocketAddress.h"
#include "Poco/Net/StreamSocket.h"
#include "Poco/Mutex.h"
#include "Poco/MongoDB/RequestMessage.h"
#include "Poco/MongoDB/ResponseMessage.h"


namespace Poco {
namespace MongoDB {


class MongoDB_API Connection
	/// Represents a connection to a MongoDB server
{
public:
	typedef Poco::SharedPtr<Connection> Ptr;

	Connection();
		/// Default constructor. Use this when you want to
		/// connect later on.

	Connection(const std::string& hostAndPort);
		/// Constructor which connects to the given MongoDB host/port.
		/// The host and port must be separated with a colon.

	Connection(const std::string& host, int port);
		/// Constructor which connects to the given MongoDB host/port.

	Connection(const Net::SocketAddress& addrs);
		/// Constructor which connects to the given MongoDB host/port.

	virtual ~Connection();
		/// Destructor

	Net::SocketAddress address() const;
		/// Returns the address of the MongoDB connection

	void connect(const std::string& hostAndPort);
		/// Connects to the given MongoDB server. The host and port must be separated
		/// with a colon.

	void connect(const std::string& host, int port);
		/// Connects to the given MongoDB server.

	void connect(const Net::SocketAddress& addrs);
		/// Connects to the given MongoDB server.

	void disconnect();
		/// Disconnects from the MongoDB server

	void sendRequest(RequestMessage& request);
		/// Sends a request to the MongoDB server
		/// Only use this when the request hasn't a response.

	void sendRequest(RequestMessage& request, ResponseMessage& response);
		/// Sends a request to the MongoDB server and receives the response.
		/// Use this when a response is expected: only a query or getmore
		/// request will return a response.

private:
	Net::SocketAddress _address;
	Net::StreamSocket _socket;
	void connect();
		/// Connects to the MongoDB server
};


inline Net::SocketAddress Connection::address() const
{
	return _address;
}


} } // namespace Poco::MongoDB


#endif //MongoDB_Connection_INCLUDED
