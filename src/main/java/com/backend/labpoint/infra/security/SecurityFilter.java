package com.backend.labpoint.infra.security;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import com.backend.labpoint.repository.UsersRepository;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;
import java.util.Arrays;

@Component
public class SecurityFilter extends OncePerRequestFilter {
    @Autowired
    private TokenService tokenService;

    @Autowired
    private UsersRepository usersRepository;

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain) throws ServletException, IOException {
        var token = recoverToken(request);
        if (token != null) {
            var subject = tokenService.validateToken(token);
            UserDetails user = usersRepository.findByRegistration(subject).orElseThrow(() -> new RuntimeException("User not found"));

            var authentication = new UsernamePasswordAuthenticationToken(user, null, user.getAuthorities());
            SecurityContextHolder.getContext().setAuthentication(authentication);
        }
        filterChain.doFilter(request, response);
    }

    private String recoverToken(HttpServletRequest request) {
        /*
        var authHeader = request.getHeader("Authorization");
        if (authHeader == null) return null;
        return authHeader.replace("Bearer ", "");
        */
        var reqCookies = request.getCookies();
        String jwtToken = null;
        if (reqCookies != null) {
            jwtToken = Arrays.stream(reqCookies)
                .filter(cookie -> cookie.getName().equals("session_jwt"))
                .map(coockie -> coockie.getValue())
                .findFirst()
                .orElse(null);
        }
        if (jwtToken != null) {
            jwtToken = jwtToken.replace("Bearer ", "");
        }
        return jwtToken;
    }
}
