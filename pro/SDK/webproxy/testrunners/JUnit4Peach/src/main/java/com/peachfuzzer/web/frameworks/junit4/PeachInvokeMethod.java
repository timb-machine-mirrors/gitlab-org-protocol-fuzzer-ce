
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.PeachState;
import com.peachfuzzer.api.PeachApiException;
import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

public class PeachInvokeMethod extends Statement {
    private final FrameworkMethod testMethod;
    private final Object target;
    private final PeachContext context;
    private final String name;

    public PeachInvokeMethod(PeachContext context, String name, FrameworkMethod testMethod, Object target) {
        this.testMethod = testMethod;
        this.target = target;
        this.context = context;
        this.name = name;
    }

    @Override
    public void evaluate() throws Throwable {

        try
        {
            context.proxy.testCase(name);

            testMethod.invokeExplosively(target);

            context.proxy.testTearDown();
        }
        catch(PeachApiException ex)
        {
            context.state = PeachState.ERROR;
            context.ex = ex;
            
            throw ex;
        }
    }
}
