
__current_route = None
__request_funcs = {}

EVENT_ACTION = 1

def register_event(kind, func):
	if kind != 1:
		return

	global __current_route;
	global __request_funcs;

	funcs = __request_funcs.get(__current_route, [])
	funcs.append(func)
	__request_funcs[__current_route] = funcs

def __set_current_route(route):
	global __current_route;
	__current_route = route

def __get_request_funcs():
	global __request_funcs;
	return '%r' % __request_funcs

def __on_request(route, context, request, body):
	global __request_funcs;
	funcs = __request_funcs.get(route, [])
	for func in funcs:
		func(context, request, body)

