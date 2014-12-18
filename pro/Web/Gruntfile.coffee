'use strict'

module.exports = (grunt) ->
	proxy = require('grunt-connect-proxy/lib/utils').proxyRequest

	grunt.initConfig
		pkg: grunt.file.readJSON 'package.json'

		bowercopy:
			libs:
				files:
					'public/lib/angles'            : 'angles:main'
					'public/lib/angular'           : 'angular:main'
					'public/lib/angular-bootstrap' : 'angular-bootstrap:main'
					'public/lib/angular-mocks'     : 'angular-mocks:main'
					'public/lib/angular-resource'  : 'angular-resource:main'
					'public/lib/angular-route'     : 'angular-route:main'
					'public/lib/angular-sanitize'  : 'angular-sanitize:main'
					'public/lib/angular-ui-select' : 'angular-ui-select:main'
					'public/lib/angular-ui-utils'  : 'angular-ui-utils:main'
					'public/lib/jquery'            : 'jquery:main'
					'public/lib/moment'            : 'moment:main'
					'public/lib/select2'           : 'select2:main'
					'public/lib/underscore'        : 'underscore:main'

			mainless:
				options:
					destPrefix: 'public/lib'
				files:
					'ace-bootstrap'               : [
						'ace-bootstrap/css/*'
						'ace-bootstrap/fonts/*'
						'ace-bootstrap/js/*'
					]
					'angular-smart-table/smart-table.js' : 'angular-smart-table/dist/smart-table.debug.js'
					'angular-tree-control/css'    : 'angular-tree-control/css/*'
					'angular-tree-control/images' : 'angular-tree-control/images/*'
					'angular-tree-control/js'     : 'angular-tree-control/angular-tree-control.js'
					'bootstrap/css'               : 'bootstrap/dist/css/bootstrap.css'
					'bootstrap/fonts'             : 'bootstrap/dist/fonts/*'
					'bootstrap/js'                : 'bootstrap/dist/js/bootstrap.js'
					'chartjs'                     : 'chartjs/Chart.js'
					'pithy'                       : 'pithy/lib/pithy.js'
					'vis/img'                     : 'vis/dist/img/*'
					'vis/vis.css'                 : 'vis/dist/vis.css'
					'vis/vis.js'                  : 'vis/dist/vis.js'

		clean:
			app: [
				'public/js/app/*'
				'public/js/test/*'
			]
			lib: [
				'public/lib'
			]

		tsd:
			refresh: 
				options:
					command: 'reinstall'
					config: 'tsd.json'

		ts:
			options:
				module: 'commonjs'
				sourceMap: true
				sourceRoot: '/ts/app'
				removeComments: false
			app:
				src: ['public/ts/app/**/*.ts']
				reference: 'public/ts/app/reference.ts'
				out: 'public/js/app/app.js'
			test:
				src: ['public/ts/test/**/*.ts']
				reference: 'public/ts/test/reference.ts'
				out: 'public/js/test/test.js'
				options:
					sourceRoot: ''
					amdloader: 'public/js/loader.js'

		jasmine:
			test:
				host: 'http://localhost:9999/'
				src: [
					# ordered libraries
					'public/lib/jquery/jquery.js'
					'public/lib/angular/angular.js'
					# unordered libraries
					'public/lib/**/*.js'
					# extra stuff
					'public/js/angular-vis.js'
					# local code
					'public/js/test/test.js'
				]
				options:
					outfile: 'public/tests.html'
					keepRunner: true

		watch:
			ts:
				files: ['public/ts/**/*.ts']
				tasks: ['compile-work']
				options:
					livereload: true
			html:
				files: ['public/**/*.html', '!public/tests.html']
				options:
					livereload: true
			css:
				files: ['public/**/*.css']
				options:
					livereload: true
			test:
				files: ['public/ts/**/*.ts']
				tasks: ['compile-test', 'run-test']
				options:
					atBegin: true

		focus:
			app:
				include: ['ts', 'html', 'css']			

		connect:
			options:
				hostname: 'localhost'
				port: 9000
				base: 'public'
				livereload: true
			proxies: [
				{context: '/p/', host: 'localhost', port: 8888}
			]
			livereload:
				options:
					middleware: (connect, options) -> 
						[ proxy, connect.static(options.base[0]) ]

		http:
			accept_eula:
				options:
					url: 'http://localhost:8888/eula'
					method: 'POST'
					form:
						accept: 'true'
			reject_eula:
				options:
					url: 'http://localhost:8888/eula'
					method: 'POST'
					form:
						accept: 'false'

		open:
			dev:
				path: 'http://localhost:<%= connect.options.port%>'

	grunt.loadNpmTasks 'grunt-bowercopy'
	grunt.loadNpmTasks 'grunt-contrib-clean'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-jasmine'
	grunt.loadNpmTasks 'grunt-contrib-uglify'
	grunt.loadNpmTasks 'grunt-contrib-watch'
	grunt.loadNpmTasks 'grunt-contrib-connect'
	grunt.loadNpmTasks 'grunt-connect-proxy'
	grunt.loadNpmTasks 'grunt-focus'
	grunt.loadNpmTasks 'grunt-http'
	grunt.loadNpmTasks 'grunt-open'
	grunt.loadNpmTasks 'grunt-ts'
	grunt.loadNpmTasks 'grunt-tsd'

	grunt.registerTask 'default', ['work']

	grunt.registerTask 'compile-work', ['ts:app']
	grunt.registerTask 'compile-test', ['ts:test']

	grunt.registerTask 'init', [
		'clean'
		'bowercopy'
		'tsd'
		'compile-work'
	]

	grunt.registerTask 'server', [
		'configureProxies'
		'http:accept_eula'
		'connect:livereload'
	]

	grunt.registerTask 'work', [
		'clean:app'
		'compile-work'
		'server'
		'open'
		'focus:app'
	]

	grunt.registerTask 'test', [
		'clean:app'
		'watch:test'
	]

	grunt.registerTask 'run-test', ['jasmine:test']
