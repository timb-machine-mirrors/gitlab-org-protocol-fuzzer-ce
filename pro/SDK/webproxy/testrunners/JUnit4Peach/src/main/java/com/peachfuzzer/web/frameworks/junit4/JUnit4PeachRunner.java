
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.PeachState;
import java.util.List;
import org.junit.Before;
import org.junit.Test;
import org.junit.internal.runners.statements.Fail;
import org.junit.runner.Description;
import org.junit.runner.notification.RunNotifier;
import org.junit.runners.BlockJUnit4ClassRunner;
import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

public class JUnit4PeachRunner extends BlockJUnit4ClassRunner {

    PeachContext _context = null;
    Description _currentDescription = null;
    
    public JUnit4PeachRunner(PeachContext context, Class<?> klass) throws org.junit.runners.model.InitializationError {
        super(klass);
        
        _context = context;
    }

    @Override
    protected void runChild(final FrameworkMethod method, RunNotifier notifier) {
        
        if(_context.debug)
            System.out.println(">> runChild: " + method.getName());
        
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
            
            for(int cnt = 0; cnt < 200 && _context.state.equals(PeachState.CONTINUE); cnt++)
            {
                runLeaf(statement, description, notifier);
            }
        }
        
        if(_context.debug)
            System.out.println("<< runChild");
    }
    
    @Override
    protected List<FrameworkMethod> computeTestMethods() {
        List<FrameworkMethod> methods = getTestClass().getAnnotatedMethods(Test.class);
        
        return methods;
    }

    @Override
    protected Statement methodInvoker(FrameworkMethod method, Object test) {
        if(_context.debug)
            System.out.println(">> methodInvoker");
        
        return new PeachInvokeMethod(_context, method.getName(), method, test);
    }
    
    @Override
    protected Statement withBefores(FrameworkMethod method, Object target,
        Statement statement) {
        
        if(_context.debug)
            System.out.println(">> withBefores");
        
        List<FrameworkMethod> befores = getTestClass().getAnnotatedMethods(
            Before.class);
        
        return befores.isEmpty() ? statement : new PeachRunBefores(_context, statement,
            befores, target);
    }
}
