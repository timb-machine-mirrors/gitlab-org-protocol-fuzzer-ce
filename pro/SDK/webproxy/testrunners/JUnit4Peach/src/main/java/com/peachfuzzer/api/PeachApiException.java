/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.peachfuzzer.api;

/**
 *
 * @author mike
 */
public class PeachApiException extends Exception {
    public String message;
    public Exception exception;
    
    public PeachApiException(String message, Exception ex)
    {
        this.message = message;
        exception = ex;
    }
}
