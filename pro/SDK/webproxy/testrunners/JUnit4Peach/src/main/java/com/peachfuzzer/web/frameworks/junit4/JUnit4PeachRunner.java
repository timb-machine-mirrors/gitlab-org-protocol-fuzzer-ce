/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.Proxy;
import java.util.List;
import org.junit.After;
import org.junit.Before;
import org.junit.internal.runners.statements.Fail;
import org.junit.runner.Description;
import org.junit.runner.notification.RunNotifier;
import org.junit.runners.BlockJUnit4ClassRunner;
import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

/**
 *
 * @author mike
 */
public class JUnit4PeachRunner extends BlockJUnit4ClassRunner {

    Proxy _proxy = null;
    Description _currentDescription = null;
    
    public JUnit4PeachRunner(Class<?> klass) throws org.junit.runners.model.InitializationError {
        super(klass);
    }

    @Override
    protected void runChild(final FrameworkMethod method, RunNotifier notifier) {
        Description description = describeChild(method);
        if (isIgnored(method)) {
            notifier.fireTestIgnored(description);
        } else {
            _currentDescription = description;
            
            Statement statement;
            try {
                statement = methodBlock(method);
            }
            catch (Throwable ex) {
                statement = new Fail(ex);
            }
            
            for(int cnt = 0; cnt < 50; cnt++)
            {
                runLeaf(statement, description, notifier);
            }
        }
    }

    @Override
    protected Statement methodInvoker(FrameworkMethod method, Object test) {
        return new PeachInvokeMethod(_proxy, _currentDescription.getClassName(), method, test);
    }
    
    @Override
    protected Statement withBefores(FrameworkMethod method, Object target,
        Statement statement) {
        List<FrameworkMethod> befores = getTestClass().getAnnotatedMethods(
            Before.class);
        
        return befores.isEmpty() ? statement : new PeachRunBefores(_proxy, statement,
            befores, target);
    }

    @Override
    protected Statement withAfters(FrameworkMethod method, Object target,
        Statement statement) {
        List<FrameworkMethod> afters = getTestClass().getAnnotatedMethods(
            After.class);
        
        return afters.isEmpty() ? statement : new PeachRunAfters(_proxy, statement, afters,
            target);
    }

}
