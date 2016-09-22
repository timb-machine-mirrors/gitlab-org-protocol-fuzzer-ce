/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.PeachApiException;
import com.peachfuzzer.api.Proxy;
import java.lang.annotation.ElementType;
import java.lang.annotation.Inherited;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import org.junit.internal.builders.AllDefaultPossibilitiesBuilder;

import org.junit.runner.Description;
import org.junit.runner.Runner;
import org.junit.runner.notification.Failure;
import org.junit.runner.notification.RunNotifier;
import org.junit.runners.ParentRunner;
import org.junit.runners.model.InitializationError;
import org.junit.runners.model.RunnerBuilder;

/**
 * Using <code>JUnit4PeachSuiteRunner</code> as a runner allows you to manually
 * build a fuzzing suite containing tests from many classes. It is the JUnit 4 equivalent of the JUnit 3.8.x
 * static {@link junit.framework.Test} <code>suite()</code> method. To use it, annotate a class
 * with <code>@RunWith(JUnit4PeachSuiteRunner.class)</code> and <code>@PeachSuiteClasses({TestClass1.class, ...})</code>.
 * When you run this class, it will run all the tests in all the suite classes.
 *
 * @since 4.0
 */
public class JUnit4PeachSuiteRunner extends ParentRunner<Runner> {
    /**
     * Returns an empty suite.
     */
    public static Runner emptySuite() {
        try {
            return new JUnit4PeachSuiteRunner((Class<?>) null, new Class<?>[0]);
        } catch (InitializationError e) {
            throw new RuntimeException("This shouldn't be possible");
        }
    }

    /**
     * The <code>SuiteClasses</code> annotation specifies the classes to be run when a class
     * annotated with <code>@RunWith(Suite.class)</code> is run.
     */
    @Retention(RetentionPolicy.RUNTIME)
    @Target(ElementType.TYPE)
    @Inherited
    public @interface PeachSuiteClasses {
        /**
         * @return the classes to be run
         */
        Class<?>[] value();
    }

    private static Class<?>[] getAnnotatedClasses(Class<?> klass) throws InitializationError {
        PeachSuiteClasses annotation = klass.getAnnotation(PeachSuiteClasses.class);
        if (annotation == null) {
            throw new InitializationError(String.format("class '%s' must have a SuiteClasses annotation", klass.getName()));
        }
        return annotation.value();
    }

    private final List<Runner> runners;
    
    protected static List<Runner> runners(Class<?>[] children) throws InitializationError
    {
        List<Runner> runners = new ArrayList<Runner>();
        
        for (Class<?> each : children) {
            Runner childRunner = new JUnit4PeachRunner(each);
            if (childRunner != null) {
                runners.add(childRunner);
            }
        }
        
        return runners;
    }

    /**
     * Called reflectively on classes annotated with <code>@RunWith(JUnit4PeachSuiteRunner.class)</code>
     *
     * @param klass the root class
     * @param builder builds runners for classes in the suite
     */
    public JUnit4PeachSuiteRunner(Class<?> klass, RunnerBuilder builder) throws InitializationError {
        this(builder, klass, getAnnotatedClasses(klass));
        //System.out.println(">> public JUnit4PeachSuiteRunner(Class<?> klass, RunnerBuilder builder)");
    }

    /**
     * Call this when there is no single root class (for example, multiple class names
     * passed on the command line to {@link org.junit.runner.JUnitCore}
     *
     * @param builder builds runners for classes in the suite
     * @param classes the classes in the suite
     */
    public JUnit4PeachSuiteRunner(RunnerBuilder builder, Class<?>[] classes) throws InitializationError {
        this(null, builder.runners(null, classes));
        //System.out.println(">> public JUnit4PeachSuiteRunner(RunnerBuilder builder, Class<?>[] classes)");
    }

    /**
     * Call this when the default builder is good enough. Left in for compatibility with JUnit 4.4.
     *
     * @param klass the root of the suite
     * @param suiteClasses the classes in the suite
     */
    protected JUnit4PeachSuiteRunner(Class<?> klass, Class<?>[] suiteClasses) throws InitializationError {
        this(new AllDefaultPossibilitiesBuilder(true), klass, suiteClasses);
        //System.out.println(">> protected JUnit4PeachSuiteRunner(Class<?> klass, Class<?>[] suiteClasses)");
    }

    /**
     * Called by this class and subclasses once the classes making up the suite have been determined
     *
     * @param builder builds runners for classes in the suite
     * @param klass the root of the suite
     * @param suiteClasses the classes in the suite
     */
    protected JUnit4PeachSuiteRunner(RunnerBuilder builder, Class<?> klass, Class<?>[] suiteClasses) throws InitializationError {
        this(klass, runners(suiteClasses));
        //System.out.println(">> protected JUnit4PeachSuiteRunner(RunnerBuilder builder, Class<?> klass, Class<?>[] suiteClasses)");
    }

    /**
     * Called by this class and subclasses once the runners making up the suite have been determined
     *
     * @param klass root of the suite
     * @param runners for each class in the suite, a {@link Runner}
     */
    protected JUnit4PeachSuiteRunner(Class<?> klass, List<Runner> runners) throws InitializationError {
        super(klass);
        this.runners = Collections.unmodifiableList(runners);
        //System.out.println(">> protected JUnit4PeachSuiteRunner(Class<?> klass, List<Runner> runners)");
    }

    @Override
    protected List<Runner> getChildren() {
        return runners;
    }

    @Override
    protected Description describeChild(Runner child) {
        return child.getDescription();
    }

    @Override
    protected void runChild(Runner runner, final RunNotifier notifier) {
        //System.out.println(">> runChild");
        
        try
        {
            Proxy proxy = new Proxy();
            proxy.sessionSetup();

            runner.run(notifier);

            proxy.sessionTearDown();
        }
        catch(PeachApiException ex)
        {
            System.out.print("Error: ");
            if(ex.message != null)
                System.out.println(ex.message);
            else
                System.out.println("Caught PeachApiException");
            
            notifier.pleaseStop();
        }
        
        //System.out.println("<< runChild");
    }
}