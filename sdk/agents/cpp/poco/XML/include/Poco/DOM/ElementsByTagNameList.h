//
// ElementsByTagNameList.h
//
// $Id: //poco/1.4/XML/include/Poco/DOM/ElementsByTagNameList.h#2 $
//
// Library: XML
// Package: DOM
// Module:  DOM
//
// Definition of the ElementsByTagNameList and ElementsByTagNameListNS classes.
//
// Copyright (c) 2004-2006, Applied Informatics Software Engineering GmbH.
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


#ifndef DOM_ElementsByTagNameList_INCLUDED
#define DOM_ElementsByTagNameList_INCLUDED


#include "Poco/XML/XML.h"
#include "Poco/DOM/NodeList.h"
#include "Poco/XML/XMLString.h"


namespace Poco {
namespace XML {


class XML_API ElementsByTagNameList: public NodeList
	// This implementation of NodeList is returned
	// by Document::getElementsByTagName() and
	// Element::getElementsByTagName().
{
public:
	Node* item(unsigned long index) const;
	unsigned long length() const;
	void autoRelease();

protected:
	ElementsByTagNameList(const Node* pParent, const XMLString& name);
	~ElementsByTagNameList();

	Node* find(const Node* pParent, unsigned long index) const;

	const Node* _pParent;
	XMLString   _name;
	mutable unsigned long _count;
	
	friend class AbstractContainerNode;
	friend class Element;
	friend class Document;
};


class XML_API ElementsByTagNameListNS: public NodeList
	// This implementation of NodeList is returned
	// by Document::getElementsByTagNameNS() and
	// Element::getElementsByTagNameNS().
{
public:
	virtual Node* item(unsigned long index) const;
	virtual unsigned long length() const;
	virtual void autoRelease();

protected:
	ElementsByTagNameListNS(const Node* pParent, const XMLString& namespaceURI, const XMLString& localName);
	~ElementsByTagNameListNS();

	Node* find(const Node* pParent, unsigned long index) const;

	const Node* _pParent;
	XMLString   _localName;
	XMLString   _namespaceURI;
	mutable unsigned long _count;
	
	friend class AbstractContainerNode;
	friend class Element;
	friend class Document;
};


} } // namespace Poco::XML


#endif // DOM_ElementsByTagNameList_INCLUDED
