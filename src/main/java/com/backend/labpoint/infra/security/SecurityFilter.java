package com.backend.labpoint.infra.security;

import com.backend.labpoint.repository.UsersRepository;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

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
        if (authHeader != null) {
            return authHeader.replace("Bearer ", "");
        }
        */

        var reqCookies = request.getCookies();
        if (reqCookies != null) {
            return Arrays.stream(reqCookies)
                    .filter(cookie -> cookie.getName().equals("session_jwt"))
                    .map(cookie -> cookie.getValue())
                    .findFirst()
                    .map(token -> token.replace("Bearer ", ""))
                    .orElse(null);
        }
        
        return null;
    }
}
