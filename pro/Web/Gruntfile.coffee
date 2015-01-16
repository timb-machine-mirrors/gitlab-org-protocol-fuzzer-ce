'use strict'

module.exports = (grunt) ->
	proxy = require('grunt-connect-proxy/lib/utils').proxyRequest

	grunt.initConfig
		pkg: grunt.file.readJSON 'package.json'

		bowercopy:
			libs:
				files:
					'public/lib/angular'           : 'angular:main'
					'public/lib/angular-bootstrap' : 'angular-bootstrap:main'
					'public/lib/angular-loading-bar' : 'angular-loading-bar:main'
					'public/lib/angular-mocks'     : 'angular-mocks:main'
					'public/lib/angular-messages'  : 'angular-messages:main'
					'public/lib/angular-sanitize'  : 'angular-sanitize:main'
					'public/lib/angular-ui-router' : 'angular-ui-router:main'
					'public/lib/angular-ui-select' : 'angular-ui-select:main'
					'public/lib/jquery'            : 'jquery:main'
					'public/lib/lodash'            : 'lodash:main'
					'public/lib/moment'            : 'moment:main'
					'public/lib/select2'           : 'select2:main'

			mainless:
				options:
					destPrefix: 'public/lib'
				files:
					'ace-bootstrap'               : [
						'ace-bootstrap/css/*'
						'ace-bootstrap/fonts/*'
					]
					'angular-chart'               : [
						'angular-chart.js/dist/angular-chart.css*'
						'angular-chart.js/angular-chart.js'
					]
					'angular-smart-table/smart-table.js' : 'angular-smart-table/dist/smart-table.debug.js'
					'angular-tree-control/css'    : 'angular-tree-control/css/*'
					'angular-tree-control/images' : 'angular-tree-control/images/*'
					'angular-tree-control/js'     : 'angular-tree-control/angular-tree-control.js'
					'bootstrap/css'               : 'bootstrap/dist/css/bootstrap.css*'
					'bootstrap/fonts'             : 'bootstrap/dist/fonts/*'
					'bootstrap/js'                : 'bootstrap/dist/js/bootstrap.js'
					'chartjs'                     : 'Chart.js/Chart.js'
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
			app: 
				options:
					command: 'reinstall'
					config: 'tsd-app.json'
			test:
				options:
					command: 'reinstall'
					config: 'tsd-test.json'

		ts:
			options:
				module: 'commonjs'
				sourceMap: true
				sourceRoot: '/ts'
				removeComments: false
			app:
				src: ['public/ts/app/**/*.ts']
				reference: 'public/ts/app/reference.ts'
				out: 'public/js/app/app.js'
			unit:
				src: ['public/ts/test/unit/**/*.ts']
				reference: 'public/ts/test/unit/reference.ts'
				out: 'public/js/test/unit.js'
			e2e:
				src: ['public/ts/test/e2e/**/*.ts']
				reference: 'public/ts/test/e2e/reference.ts'
				out: 'public/js/test/e2e.js'

		jasmine:
			test:
				host: 'http://localhost:9999/'
				src: [
					'public/js/test/unit.js'
				]
				options:
					outfile: 'public/tests.html'
					keepRunner: true
					vendor: [
						# ordered libraries
						'public/lib/jquery/jquery.js'
						'public/lib/chartjs/Chart.js'
						'public/lib/angular/angular.js'
						# unordered libraries
						'public/lib/**/*.js'
						# extra stuff
						'public/js/angular-vis.js'
						'node_modules/jasmine-custom-message/jasmine-custom-message.js'
					]

		protractor:
			options:
				configFile: 'protractor.conf.js'
			run:
				options:
					keepAlive: false
			continuous:
				options:
					keepAlive: true

		watch:
			ts:
				files: ['public/ts/app/**/*.ts']
				tasks: ['ts:app']
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
			unit:
				files: ['public/ts/app/**/*.ts', 'public/ts/test/unit/**/*.ts']
				tasks: ['ts:unit', 'jasmine:test']
				options:
					atBegin: true
			e2e:
				files: ['public/ts/app/**/*.ts', 'public/ts/test/e2e/**/*.ts']
				tasks: ['ts:e2e', 'protractor:continuous']
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
				middleware: (connect, options) -> 
					[ proxy, connect.static(options.base[0]) ]
			proxies: [
				{context: '/p/', host: 'localhost', port: 8888}
			]
			livereload:
				options:
					livereload: true
			test:
				options:
					port: 9001
					livereload: false

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
	grunt.loadNpmTasks 'grunt-protractor-runner'
	grunt.loadNpmTasks 'grunt-ts'
	grunt.loadNpmTasks 'grunt-tsd'

	grunt.registerTask 'default', ['work']

	grunt.registerTask 'compile', ['ts:app']

	grunt.registerTask 'init', [
		'clean'
		'bowercopy'
		'tsd'
		'ts:app'
	]

	grunt.registerTask 'server', [
		'configureProxies'
		'http:accept_eula'
		'connect:livereload'
	]

	grunt.registerTask 'work', [
		'clean:app'
		'ts:app'
		'server'
		'open'
		'focus:app'
	]

	grunt.registerTask 'unit', [
		'watch:unit'
	]

	grunt.registerTask 'e2e', [
		'ts:app'
		'ts:e2e'
		'configureProxies'
		'http:accept_eula'
		'connect:test'
		'watch:e2e'
	]

	grunt.registerTask 'run-test', ['jasmine:test']
