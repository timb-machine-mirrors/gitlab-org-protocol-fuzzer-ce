/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.web.frameworks.junit4;

import com.peachfuzzer.api.Proxy;
import java.util.ArrayList;
import java.util.List;

import org.junit.runners.model.FrameworkMethod;
import org.junit.runners.model.MultipleFailureException;
import org.junit.runners.model.Statement;

public class PeachRunAfters extends Statement {
    private final Statement next;
    private final Object target;
    private final List<FrameworkMethod> afters;
    private final Proxy proxy;

    public PeachRunAfters(Proxy proxy, Statement next, List<FrameworkMethod> afters, Object target) {
        this.next = next;
        this.afters = afters;
        this.target = target;
        this.proxy = proxy;
    }

    @Override
    public void evaluate() throws Throwable {
        List<Throwable> errors = new ArrayList<Throwable>();
        try {
            next.evaluate();
        } catch (Throwable e) {
            errors.add(e);
        } finally {
            
            proxy.testTearDown();
            
            for (FrameworkMethod each : afters) {
                try {
                    each.invokeExplosively(target);
                } catch (Throwable e) {
                    errors.add(e);
                }
            }
        }
        
        MultipleFailureException.assertEmpty(errors);
    }
}