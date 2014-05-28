module.exports = function(config){
  config.set({

    basePath : '../',

    files : [
      // libs, includes, etc
      'bower_components/angular/angular.js',
      'bower_components/angular-route/angular-route.js',
      'bower_components/angular-resource/angular-resource.js',
      'bower_components/angular-mocks/angular-mocks.js',
      'bower_components/angular-animate/angular-animate.js',
      'bower_components/jQuery/dist/jquery.js',
      'bower_components/ng-grid/build/ng-grid.js',
      'Content/js/line-chart.min.js',
      'Scripts/angular-poller.js',
      'Scripts/angular-local-storage.js',

      // our code, application and test
      'app/js/**/*.js',
      'app/ts/**/*.js',
      'test/unit/**/*.js'
    ],

    autoWatch : true,

    frameworks: ['jasmine'],

    browsers : ['Chrome'],

    plugins : [
            'karma-chrome-launcher',
            'karma-firefox-launcher',
            'karma-jasmine'
            ],

    junitReporter : {
      outputFile: 'test_out/unit.xml',
      suite: 'unit'
    }

  });
};
