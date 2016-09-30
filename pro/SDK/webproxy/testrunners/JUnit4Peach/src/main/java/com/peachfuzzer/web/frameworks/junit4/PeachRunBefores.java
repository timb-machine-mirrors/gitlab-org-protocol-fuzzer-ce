/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.PeachState;
import com.peachfuzzer.api.PeachApiException;
import java.util.List;

import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.Statement;

public class PeachRunBefores extends Statement {
    private final Statement next;
    private final Object target;
    private final List<FrameworkMethod> befores;
    private final PeachContext context;

    public PeachRunBefores(PeachContext context, Statement next, List<FrameworkMethod> befores, Object target) {
        this.next = next;
        this.befores = befores;
        this.target = target;
        this.context = context;
    }

    @Override
    public void evaluate() throws Throwable {
        
        try
        {
            context.proxy.testSetUp();
        }
        catch(PeachApiException ex)
        {
            context.state = PeachState.ERROR;
            context.ex = ex;
            
            throw ex;
        }
        
        for (FrameworkMethod before : befores) {
            before.invokeExplosively(target);
        }
        next.evaluate();
    }
}