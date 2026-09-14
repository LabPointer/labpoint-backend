package com.backend.labpoint.infra.security;

import com.backend.labpoint.repository.AccountRepository;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.util.AntPathMatcher;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.Arrays;
import java.util.List;

@Component
public class SecurityFilter extends OncePerRequestFilter {

    private static final List<String> PUBLIC_ROUTES = Arrays.asList(
            "/docs/**",
            "/v3/**",
            "/swagger-ui/**",
            "/auth/sign-in",
            "/auth/sign-up",
            "/auth/sign-out",
            "/auth/forgot-password",
            "/auth/reset-password/**"
    );

    private static final AntPathMatcher PATH_MATCHER = new AntPathMatcher();

    @Autowired
    private TokenService tokenService;

    @Autowired
    private AccountRepository usersRepository;

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {

        boolean isPublicRoute = isPublicRoute(request.getServletPath());
        String token = recoverToken(request);

        if (token != null && !isPublicRoute) {
            String subject = tokenService.validateToken(token);

            if (subject != null && !subject.isBlank()) {
                usersRepository.findByRegistration(subject).ifPresent(user -> {
                    UsernamePasswordAuthenticationToken authentication =
                            new UsernamePasswordAuthenticationToken(user, null, user.getAuthorities());
                    SecurityContextHolder.getContext().setAuthentication(authentication);
                });
            }
            // Token inválido/expirado ou usuário não encontrado: não autentica.
            // Deixa a cargo do SecurityConfiguration decidir se a rota exige autenticação,
            // evitando lançar exceção dentro do filtro para rotas públicas ou tokens expirados.
        }

        filterChain.doFilter(request, response);
    }

    private boolean isPublicRoute(String path) {
        return PUBLIC_ROUTES.stream().anyMatch(pattern -> PATH_MATCHER.match(pattern, path));
    }

    private String recoverToken(HttpServletRequest request) {
        Cookie[] reqCookies = request.getCookies();
        if (reqCookies != null) {
            return Arrays.stream(reqCookies)
                    .filter(cookie -> cookie.getName().equals("jwt-session"))
                    .map(Cookie::getValue)
                    .findFirst()
                    .map(token -> token.replace("Bearer ", ""))
                    .orElse(null);
        }

        return null;
    }
}