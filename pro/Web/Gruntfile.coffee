'use strict'

module.exports = (grunt) ->
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
					'public/lib/angular-ui-utils'  : 'angular-ui-utils:main'
					'public/lib/jquery'            : 'jquery:main'
					'public/lib/moment'            : 'moment:main'
					'public/lib/ng-grid'           : 'ng-grid:main'
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
					'chosen/chosen.css'           : 'chosen-build/chosen.css'
					'chosen/chosen.jquery.js'     : 'chosen-build/chosen.jquery.js'
					'bootstrap/css'               : 'bootstrap/dist/css/bootstrap.css'
					'bootstrap/fonts'             : 'bootstrap/dist/fonts/*'
					'bootstrap/js'                : 'bootstrap/dist/js/bootstrap.js'
					'chartjs'                     : 'chartjs/Chart.js'
					'ng-grid/plugins'             : 'ng-grid/plugins/ng-grid-flexible-height.js'
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
			watch:
				src: ['public/ts/app/**/*.ts']
				reference: 'public/ts/app/reference.ts'
				out: 'public/js/app/app.js'
				watch: 'public/ts'
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
			test:
				files: ['public/ts/**/*.ts']
				tasks: ['compile-test', 'run-test']
				options:
					atBegin: true

	grunt.loadNpmTasks 'grunt-bowercopy'
	grunt.loadNpmTasks 'grunt-contrib-clean'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-jasmine'
	grunt.loadNpmTasks 'grunt-contrib-uglify'
	grunt.loadNpmTasks 'grunt-contrib-watch'
	grunt.loadNpmTasks 'grunt-ts'
	grunt.loadNpmTasks 'grunt-tsd'

	grunt.registerTask 'default', ['work']

	grunt.registerTask 'init', ['clean', 'bowercopy', 'tsd', 'compile-work']

	grunt.registerTask 'compile-work', ['ts:app']
	grunt.registerTask 'compile-test', ['ts:test']

	grunt.registerTask 'work-and-watch', ['ts:watch']
	grunt.registerTask 'test-and-watch', ['watch:test']

	grunt.registerTask 'run-test', ['jasmine:test']

	grunt.registerTask 'work', ['clean:app', 'work-and-watch']
	grunt.registerTask 'test', ['clean:app', 'test-and-watch']
